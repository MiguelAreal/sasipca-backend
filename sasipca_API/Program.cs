using DotNetEnv;
using Serilog;
using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using sasipca_API.DBModels;
using sasipca_API.Hubs;
using sasipca_API.Middleware;
using sasipca_API.Models;
using sasipca_API.Services;
using sasipca_API.Services.Interfaces;
using Serilog;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using System.Transactions;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;
namespace sasipca_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.UTF8;
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-PT");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pt-PT");

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console() // Continua a mostrar na consola
            .WriteTo.File("Logs/sasipca-log-.txt", // Cria pasta Logs na raiz
                rollingInterval: RollingInterval.Day, // Cria um ficheiro novo por dia
                retainedFileCountLimit: 7) // Guarda apenas os últimos 7 dias
            .CreateLogger();


            Env.Load();
            // Busca variáveis de ambiente de Base de dados e JWT
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_KEY");
            var azureClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

            // Verifica se as chaves estão presentes
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT_KEY is not set in environment variables.");
            }
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DB_CONNECTION_KEY is not set in environment variables.");
            }
            if (string.IsNullOrEmpty(azureClientId))
            {
                throw new InvalidOperationException("AZURE_CLIENT_ID is not set in environment variables.");
            }

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();

            //Adicionar dependências de Serviços.
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<ImageProcessingService>();
            builder.Services.AddScoped<IJobSchedulerService,JobSchedulerService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IBeneficiaryService, BeneficiaryService>();
            builder.Services.AddScoped<IDeliveryService, DeliveryService>();
            builder.Services.AddScoped<IReportingService, ReportingService>();
            builder.Services.AddScoped<ITemplateGeneratorService, TemplateGeneratorService>();
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();
            builder.Services.AddScoped<IJWTService, JWTService>();
            builder.Services.AddTransient<IEmailService, EmailService>();
            builder.Services.AddTransient<ITypesService, TypesService>();
            builder.Services.AddSingleton<IConverter, SynchronizedConverter>(provider => new SynchronizedConverter(new PdfTools()));

            //Adicionar Serviço de WebSocket
            builder.Services.AddWebSockets(options => {
                options.KeepAliveInterval = TimeSpan.FromMinutes(2);
            });

            // Regista o DBContext para usar a string de Conexão
            builder.Services.AddDbContext<SasipcaContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            //Regista o serviço HangFire.
            builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseStorage(
                new MySqlStorage(
                    connectionString,
                    new MySqlStorageOptions
                    {
                        // Configurações recomendadas para evitar Timeouts em tabelas grandes
                        TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                        QueuePollInterval = TimeSpan.FromSeconds(15),
                        JobExpirationCheckInterval = TimeSpan.FromHours(1),
                        CountersAggregateInterval = TimeSpan.FromMinutes(5),
                        PrepareSchemaIfNecessary = true, // Cria as tabelas do Hangfire automaticamente
                        DashboardJobListLimit = 5000,
                        TransactionTimeout = TimeSpan.FromMinutes(1),
                        TablesPrefix = "Hangfire_" // Prefixo para não misturar com as outras tabelas
                    }
                )
            ));
            builder.Services.AddHangfireServer();
            builder.Services.AddSignalR();


            // Regista Serviço de autenticação JWT.
            var key = Encoding.UTF8.GetBytes(jwtKey);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        //Para Websocket SignalR
                        var accessToken = context.Request.Query["access_token"];

                        // Se o request for para nosso hub SignalR...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/notification-hub"))
                        {
                            // Lê o token do query string
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },

                    OnChallenge = async context =>
                    {
                        // Impede o comportamento padrão
                        context.HandleResponse();

                        string errorCode = "invalid_token";

                        if (context.AuthenticateFailure != null)
                        {
                            if (context.AuthenticateFailure is SecurityTokenExpiredException)
                            {
                                errorCode = "expired_token";

                                context.Response.Headers["WWW-Authenticate"] =
                                @"Bearer error=""expired_token"", error_description=""The access token expired""";

                            }
                        }

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            error = errorCode,
                        }));
                    }
                };
            });

            // Adicionar a política de CORS para produção
            /*builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.SetIsOriginAllowed(origin =>
                        origin.StartsWith("http://localhost") || origin.EndsWith(".azurestaticapps.net"))
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });*/

            //DESENVOLVIMENTO
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.SetIsOriginAllowed(_ => true) // <--- O SEGREDO É ISTO
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // Necessário para SignalR e Auth
                });
            });

            // Rate Limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        key => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 45, // Máximo de 45 requisições...
                            Window = TimeSpan.FromSeconds(60), // ...a cada 60 segundos
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 2 // Se o limite for atingido, mais 2 requisições podem ser enfileiradas
                        }));
            });

            // Suprime erros automáticos quando um input não corresponde ao modelo [required]
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<HttpClaim>();
            })
             .AddJsonOptions(options =>
             {
                 options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
                 options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
             });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations();
                options.IncludeXmlComments(Path.Combine(
                AppContext.BaseDirectory,
                $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"), true);

                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "sasipca API",
                    Version = "1.0",
                    Description = "API para a aplicação sasipca"
                });

                // Configurar suporte para autenticação JWT no Swagger
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Insira 'Bearer' seguido de um espaço e o token JWT. Exemplo: Bearer SEU_TOKEN_AQUI"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                    Array.Empty<string>()
                }});

                options.CustomSchemaIds(type =>
                {
                    var name = type.FullName?.Replace("+", ".") ?? type.Name;

                    if (name.StartsWith("sasipca_API."))
                    {
                        name = name.Substring("sasipca_API.".Length);
                    }

                    return name;
                });

            });

           

            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "sasipca API v1");

                    // Permite que o Swagger envie cookies junto com as requisições
                    c.ConfigObject.AdditionalItems["requestCredentials"] = "inclsude";
                });
                app.UseHangfireDashboard();
            }

            // Definir a pasta raíz de uploads
            const string StorageRootFolder = "Storage";

            // Verifica e cria a pasta 'Storage' se não existir
            string storagePath = Path.Combine(app.Environment.ContentRootPath, StorageRootFolder);

            if (!Directory.Exists(storagePath))
            {
                // A lógica para criar o diretorio no sistema de ficheiros
                Directory.CreateDirectory(storagePath);
            }

            // Mapear a pasta "Storage" para a URL "/static"
            app.UseStaticFiles(new StaticFileOptions
            {
                // O RequestPath define o prefixo URL que o cliente usará (ex: https://api.exemplo.com/static/CampaignImages/imagem.jpg)
                FileProvider = new PhysicalFileProvider(storagePath),
                RequestPath = "/static"
            });


            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseWebSockets();
            app.MapHub<NotificationHub>("/notification-hub");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.MapControllers();
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            app.Run();
        }
    }
}
