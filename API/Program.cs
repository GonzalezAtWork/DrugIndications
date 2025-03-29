using DrugIndications.Application.Interfaces;
using DrugIndications.Application.UseCases;
using DrugIndications.Infrastructure.Auth;
using DrugIndications.Infrastructure.Repositories;
using DrugIndications.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;

namespace DrugIndications.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // Add services to the container
            ConfigureServices(builder.Services, builder.Configuration);
            
            var app = builder.Build();

            // Ensure database exists before app starts
            EnsureDatabaseExists(app.Services);

            // Configure the HTTP request pipeline
            ConfigureMiddleware(app, app.Environment);
            
            app.Run();
        }
        private static void EnsureDatabaseExists(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);

            // Store the database name and then change connection string to connect to master
            var databaseName = connectionStringBuilder.InitialCatalog;
            connectionStringBuilder.InitialCatalog = "master";

            using (var connection = new SqlConnection(connectionStringBuilder.ConnectionString))
            {
                connection.Open();

                // Check if database exists
                var checkDbCommand = new SqlCommand($"SELECT COUNT(*) FROM sys.databases WHERE name = '{databaseName}'", connection);
                var databaseExists = (int)checkDbCommand.ExecuteScalar() > 0;

                if (!databaseExists)
                {
                    // Read SQL script from embedded resource or file
                    var sqlScript = File.ReadAllText("Infrastructure/DatabaseSchema.sql");

                    // Split the script on GO statements and execute each batch
                    var batches = sqlScript.Split(new[] { "GO", "go" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var batch in batches)
                    {
                        if (!string.IsNullOrWhiteSpace(batch))
                        {
                            using (var batchCommand = new SqlCommand(batch, connection))
                            {
                                batchCommand.ExecuteNonQuery();
                            }
                        }
                    }

                    Console.WriteLine($"Database '{databaseName}' created successfully."); ;
                }
                else
                {
                    Console.WriteLine($"Database '{databaseName}' already exists.");
                }
            }
        }
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add controllers
            services.AddControllers();
            
            // Add Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Drug Indications API", Version = "v1" });
                
                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                    }
                });
            });
            
            // Add JWT Authentication
            var jwtSecret = configuration["Jwt:Secret"];
            var key = Encoding.ASCII.GetBytes(jwtSecret);
            
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
            
            // Add Authorization
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
            });
            
            // Register dependencies
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Register repositories
            services.AddScoped<IDrugRepository>(_ => new DrugRepository(connectionString));
            services.AddScoped<IIndicationRepository>(_ => new IndicationRepository(connectionString));
            services.AddScoped<ICopayProgramRepository>(_ => new CopayProgramRepository(connectionString));

            // Register services
            services.AddScoped<IDailyMedService, DailyMedService>();
            services.AddScoped<IICD10MappingService, ICD10MappingService>();
            services.AddScoped<IEligibilityParser>(_ =>
                new EligibilityParser(configuration["OpenAI:ApiKey"]));
            services.AddScoped<IAuthService>(_ =>
                new AuthService(
                    connectionString,
                    configuration["Jwt:Secret"],
                    int.Parse(configuration["Jwt:ExpirationMinutes"])));

            // Use cases
            services.AddScoped<ExtractDrugIndicationsUseCase>();
            services.AddScoped<ProcessCopayCardUseCase>();
        }
        private static void ConfigureMiddleware(WebApplication app, IWebHostEnvironment env)
        {
            // Enable Swagger in development
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            // Use HTTPS redirection
            app.UseHttpsRedirection();

            // Add redirect from root to Swagger UI
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.Value == "/")
                {
                    // Redirect to Swagger UI
                    context.Response.Redirect("/swagger");
                    return;
                }

                await next();
            });

            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        message = "Unauthorized. Please login to access this resource."
                    }));
                });
            });

            // Enable authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();
            
            // Map controllers
            app.MapControllers();
        }
    }
}