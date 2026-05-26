using DotNetEnv;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
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
                .WriteTo.Console()
                .WriteTo.File("Logs/sasipca-log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            Env.Load();
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_KEY");
            var azureClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(azureClientId))
            {
                throw new InvalidOperationException("Variáveis de ambiente essenciais (JWT_KEY, DB_CONNECTION_KEY, AZURE_CLIENT_ID) não estão configuradas.");
            }

            var firebaseCredentialPath = "sasipca-2ea18-firebase-adminsdk-fbsvc-5d72cf6e66.json";
            if (File.Exists(firebaseCredentialPath))
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(firebaseCredentialPath)
                });
            }

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();

            // Dependências de Serviços
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<ImageProcessingService>();
            builder.Services.AddScoped<IJobSchedulerService, JobSchedulerService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IBeneficiaryService, BeneficiaryService>();
            builder.Services.AddScoped<IDeliveryService, DeliveryService>();
            builder.Services.AddScoped<IReportingService, ReportingService>();
            builder.Services.AddScoped<ITemplateGeneratorService, TemplateGeneratorService>();
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();
            builder.Services.AddScoped<IJWTService, JWTService>();
            builder.Services.AddTransient<ITypesService, TypesService>();

            builder.Services.AddWebSockets(options => {
                options.KeepAliveInterval = TimeSpan.FromMinutes(2);
            });

            builder.Services.AddDbContext<SasipcaContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseStorage(new MySqlStorage(connectionString, new MySqlStorageOptions
                {
                    TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    PrepareSchemaIfNecessary = true,
                    TablesPrefix = "Hangfire_"
                })));
            builder.Services.AddHangfireServer();
            builder.Services.AddSignalR();

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
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notification-hub"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.SetIsOriginAllowed(origin =>
                    {
                        if (string.IsNullOrWhiteSpace(origin)) return false;
                        if (origin.Contains("localhost") || origin.EndsWith("rapi.tail1fcae6.ts.net") || origin.EndsWith("rapi4real.duckdns.org")) return true;
                        return false;
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            });

            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        key => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 70,
                            Window = TimeSpan.FromSeconds(60),
                            QueueLimit = 2
                        }));
            });

            builder.Services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddControllers(options => { options.Filters.Add<HttpClaim>(); })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "sasipca API", Version = "1.0" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
                });
            });

            var app = builder.Build();

            // --- CONFIGURAÇÃO DE PROXY ---
            var forwardOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardOptions.KnownNetworks.Clear();
            forwardOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardOptions);

            app.Use((context, next) =>
            {
                if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto))
                {
                    context.Request.Scheme = proto;
                }
                return next();
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "sasipca API v1"));
                app.UseHangfireDashboard();
            }

            // Arquivos Estáticos
            var contentRoot = app.Environment.ContentRootPath;
            var campaignImagesPath = Path.Combine(contentRoot, "Storage", "CampaignImages");
            if (!Directory.Exists(campaignImagesPath)) Directory.CreateDirectory(campaignImagesPath);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(campaignImagesPath),
                RequestPath = "/api/static/CampaignImages"
            });

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.UseWebSockets();
            app.MapHub<NotificationHub>("/api/notification-hub");
            app.MapControllers();

            app.Run();
        }
    }
}