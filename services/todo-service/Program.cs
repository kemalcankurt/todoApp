using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

using todo_service.Config;
using todo_service.Data;
using todo_service.Repositories;
using todo_service.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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

// 5. Middleware Pipeline
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
        });

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

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<TodoDBContext>("Database")
        .AddCheck("Self", () => HealthCheckResult.Healthy());

    // Database
    builder.Services.AddDbContext<TodoDBContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(3)
        ));

    // AutoMapper
    builder.Services.AddAutoMapper(typeof(Program));

    // Application Services
    builder.Services.AddScoped<ITodoRepository, TodoRepository>();
    builder.Services.AddScoped<ITodoService, TodoService>();
    builder.Services.AddSingleton<JwtService>();
}

void ConfigureAuth(WebApplicationBuilder builder)
{
    var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
        ?? throw new InvalidOperationException("JWT configuration is missing!");

    var key = Encoding.UTF8.GetBytes(jwtOptions.Secret);

    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    });
}

void ConfigureMiddleware(WebApplication app)
{
    app.MapHealthChecks("/healthz");

    // Auto-migrate database on startup
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<TodoDBContext>();
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration Error: {ex.Message}");
        }
    }

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
}



