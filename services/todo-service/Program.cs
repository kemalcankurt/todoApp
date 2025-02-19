using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;

using todo_service.Config;
using todo_service.Data;
using todo_service.Repositories;
using todo_service.Services;
using todo_service.Swagger;
using todo_service.Mappings;

var builder = WebApplication.CreateBuilder(args);
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

// 1. Configuration
ConfigureConfiguration(builder);

// 2. Logging & Telemetry
ConfigureLogging(builder);
ConfigureTelemetry(builder);

// 3. Services
ConfigureServices(builder);

// 4. Authentication & Authorization
ConfigureAuth(builder);

var app = builder.Build();

// 5. Database
await ConfigureDatabase(app);

// 6. Middleware Pipeline
ConfigureMiddleware(app);

await app.RunAsync();

// Helper Methods
void ConfigureConfiguration(WebApplicationBuilder builder)
{
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{environment}.json", optional: true)
        .AddEnvironmentVariables();
}

void ConfigureLogging(WebApplicationBuilder builder)
{
    builder.Host.UseSerilog((ctx, lc) => lc
        .MinimumLevel.Debug()
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .WriteTo.Console(
            Serilog.Events.LogEventLevel.Debug,
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        ));

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.ParseStateValues = true;
        logging.AddOtlpExporter(options => options.Endpoint = new Uri("http://otel-collector:4317"));
    });
}

void ConfigureTelemetry(WebApplicationBuilder builder)
{
    const string SERVICE_NAME = "todo-service";
    var resourceBuilder = ResourceBuilder.CreateDefault()
        .AddService(SERVICE_NAME)
        .AddAttributes(new Dictionary<string, object>
        {
            ["service.name"] = SERVICE_NAME,
            ["service.namespace"] = "todoapp",
            ["service.version"] = "1.0.0",
            ["service.instance.id"] = Guid.NewGuid().ToString(),
            ["deployment.environment"] = environment,
            ["host.name"] = Environment.MachineName,
            ["container.name"] = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown"
        })
        .AddTelemetrySdk()
        .AddEnvironmentVariableDetector();

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProvider =>
        {
            tracerProvider
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                })
                .AddSqlClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.SetDbStatementForText = true;
                })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(options => options.Endpoint = new Uri("http://otel-collector:4317"));
        })
        .WithMetrics(metricsProvider =>
        {
            metricsProvider
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddSqlClientInstrumentation()
                .AddMeter("Microsoft.EntityFrameworkCore")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://otel-collector:4317");
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                });
        });
}

void ConfigureServices(WebApplicationBuilder builder)
{
    // API Controllers
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    ConfigureSwagger(builder);

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<TodoDBContext>("Database")
        .AddCheck("Self", () => HealthCheckResult.Healthy());

    // Database
    if (environment != "Testing")
    {
        builder.Services.AddDbContext<TodoDBContext>(options =>
        {
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(3)
                 );
        });
    }

    // AutoMapper
    builder.Services.AddAutoMapper(typeof(MappingProfile));

    // HTTP Context
    builder.Services.AddHttpContextAccessor();

    // Application Services
    builder.Services.AddScoped<ITodoRepository, TodoRepository>();
    builder.Services.AddScoped<ITodoService, TodoService>();
    builder.Services.AddSingleton<JwtService>();

    // Configuration Options
    builder.Services.Configure<JwtOptions>(
        builder.Configuration.GetSection("Jwt"));
}

void ConfigureAuth(WebApplicationBuilder builder)
{
    // JWT Configuration
    var jwtOptions = builder.Configuration
        .GetSection("Jwt")
        .Get<JwtOptions>()
        ?? throw new InvalidOperationException("JWT configuration is missing");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    });
}

void ConfigureMiddleware(WebApplication app)
{
    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
    }

    // Health Check Endpoint
    app.MapHealthChecks("/healthz", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                Status = report.Status.ToString(),
                HealthChecks = report.Entries.Select(e => new
                {
                    Component = e.Key,
                    Status = e.Value.Status.ToString(),
                    Description = e.Value.Description
                })
            });
        }
    }).AllowAnonymous();

    // Core Middleware
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
}

async Task ConfigureDatabase(WebApplication app)
{
    if (environment != "Testing")
    {
        using var scope = app.Services.CreateScope();
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<TodoDBContext>();
            await context.Database.MigrateAsync();
            Log.Information("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating the database");
            throw;
        }
    }
}

void ConfigureSwagger(WebApplicationBuilder builder)
{
    if (environment != "Production")
    {
        var swaggerConfig = builder.Configuration.GetSection("Swagger").Get<ApiSwaggerOptions>();

        if (swaggerConfig == null) return;

        builder.Services.AddSwaggerGen(options =>
        {
            // Load API metadata from configuration
            options.SwaggerDoc(swaggerConfig.Version, new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = swaggerConfig.Title,
                Version = swaggerConfig.Version,
                Description = swaggerConfig.Description,
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = swaggerConfig.Contact?.Name,
                    Email = swaggerConfig.Contact?.Email
                }
            });

            // Load JWT Security configuration
            var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = swaggerConfig.Security?.Bearer?.Name,
                Description = swaggerConfig.Security?.Bearer?.Description,
                In = Enum.Parse<Microsoft.OpenApi.Models.ParameterLocation>(swaggerConfig.Security?.Bearer?.In),
                Type = Enum.Parse<Microsoft.OpenApi.Models.SecuritySchemeType>(swaggerConfig.Security?.Bearer?.Type),
                Scheme = swaggerConfig.Security?.Bearer?.Scheme,
                BearerFormat = swaggerConfig.Security?.Bearer?.BearerFormat
            };

            options.AddSecurityDefinition("Bearer", securityScheme);
            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
        { securityScheme, Array.Empty<string>() }
            });
        });
    }
}
