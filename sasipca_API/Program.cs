using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using sasipca_API.Models;
using sasipca_API.Services;
using System.Text;
using DotNetEnv;
using System.Reflection;
using sasipca_API.Data;
using Hangfire;
using sasipca_API.Middleware;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.WebSockets;
using sasipca_API.Hubs;
using sasipca_API.Services.Interfaces;
using sasipca_API.DBModels;
using Renci.SshNet;

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

            Env.Load();
            // Busca variáveis de ambiente de Base de dados e JWT
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_KEY");
            var azureStorageKey = Environment.GetEnvironmentVariable("AZURE_STORAGE_KEY");

            // Variáveis do túnel SSH (Para acesso a servidor de Base de Dados)
            var sshHost = Environment.GetEnvironmentVariable("SSH_HOST");
            var sshPort = int.Parse(Environment.GetEnvironmentVariable("SSH_PORT") ?? "2222");
            var sshUser = Environment.GetEnvironmentVariable("SSH_USER");
            var sshPassword = Environment.GetEnvironmentVariable("SSH_PASSWORD");
            var localPort = uint.Parse(Environment.GetEnvironmentVariable("SSH_LOCAL_PORT") ?? "3306");

            // Verifica se a chave JWT e a string de conexão estão presentes
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT_KEY is not set in environment variables.");
            }
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DB_CONNECTION_KEY is not set in environment variables.");
            }
            if (string.IsNullOrEmpty(azureStorageKey))
            {
                throw new InvalidOperationException("AZURE_STORAGE_KEY is not set in environment variables.");
            }

            // Criar túnel SSH antes de iniciar a app
            var sshClient = new SshClient(sshHost, sshPort, sshUser, sshPassword);
            sshClient.Connect();
            Console.WriteLine($"SSH conectado a {sshHost}:{sshPort}");

            var portForward = new ForwardedPortLocal("127.0.0.1", localPort, "127.0.0.1", 3306);
            sshClient.AddForwardedPort(portForward);
            portForward.Start();


            var builder = WebApplication.CreateBuilder(args);

            //Adicionar dependências de Serviços.
            builder.Services.AddScoped<INotificacaoService,NotificacaoService>();
            builder.Services.AddSingleton<AzureStorageService>();
            builder.Services.AddScoped<ImageProcessingService>();
            builder.Services.AddScoped<JobSchedulerService>();
            builder.Services.AddScoped<AnuncioService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAnuncioService, AnuncioService>();
            builder.Services.AddScoped<IJWTService, JWTService>();
            builder.Services.AddTransient<IEmailService, EmailService>();


            //Adicionar Serviço de WebSocket
            builder.Services.AddWebSockets(options => {
                options.KeepAliveInterval = TimeSpan.FromMinutes(2);
            });


            // Regista o DbContext para usar a string de conexão (Neighbourlink)
            builder.Services.AddDbContext<NLDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Regista o DBContext para usar a string de Conexão
            builder.Services.AddDbContext<SasipcaContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));


            //Regista o serviço HangFire.
            /*builder.Services.AddHangfire(config =>
            config.UseSqlServerStorage(connectionString, new Hangfire.SqlServer.SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = true // Garante que as tabelas sejam criadas na base de dados
            }));


            builder.Services.AddHangfireServer();*/
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

            // Adicionar a política de CORS para Desevolvimento e produção
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.SetIsOriginAllowed(origin =>
                        origin.StartsWith("http://localhost") || origin.EndsWith(".azurestaticapps.net")|| origin.EndsWith("neighbourlink.pt"))
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // Implementação de Rate Limiting
            // Ajuda a prevenir abuso, DDoS e custos excessivos no Azure.
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        key => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30, // Máximo de 30 requisições...
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
                    c.ConfigObject.AdditionalItems["requestCredentials"] = "include";
                });

            }


            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseWebSockets();
            app.MapHub<NotificationHub>("/notification-hub");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.MapControllers();

            // Fecha o túnel SSH quando a app termina
            app.Lifetime.ApplicationStopping.Register(() =>
            {
                try
                {
                    portForward.Stop();
                    sshClient.Disconnect();
                    sshClient.Dispose();
                    Console.WriteLine("Túnel SSH fechado.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao encerrar túnel SSH: {ex.Message}");
                }
            });
            app.Run();
        }
    }
}
