using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AspNetCoreRateLimit;
using NiceCleanLib.Data;
using NiceCleanLib.Services.Interfaces;
using NiceCleanLib.Services.Repositories;
using NiceCleanREST.Services;
using NiceCleanREST.Middleware;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// OpenAPI/Swagger documentation
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// Database configuration with connection pooling
var connectionString = builder.Configuration.GetConnectionString("NiceClean");
builder.Services.AddDbContext<NiceCleanDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mysqlOptions =>
        {
            // Connection pooling for production efficiency
            mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelaySeconds: 5,
                errorNumbersToAdd: null);
        }
    )
);

// Environment-specific repository registration
if (builder.Environment.IsProduction())
{
    // Production: Use database repositories
    builder.Services.AddScoped<IPinRepository, PinRepositoryDB>();
    builder.Services.AddScoped<IUserRepository, UserRepositoryDB>();
    builder.Services.AddScoped<IPinVoteRepository, PinVoteRepositoryDB>();
    builder.Services.AddScoped<IEventRepository, EventRepositoryDB>();
    builder.Services.AddScoped<IReportRepository, ReportRepositoryDB>();
}
else
{
    // Development: Use in-memory repositories for testing (with dummy data removed in migration strategy)
    builder.Services.AddSingleton<IPinRepository, PinRepository>();
    builder.Services.AddSingleton<IUserRepository, UserRepository>();
    builder.Services.AddSingleton<IPinVoteRepository, PinVoteRepository>();
    builder.Services.AddSingleton<IEventRepository, EventRepository>();
    builder.Services.AddSingleton<IReportRepository, ReportRepository>();
}

// Authentication service for JWT and password hashing
builder.Services.AddSingleton<IAuthService, AuthService>();

// JWT Authentication Configuration
var jwtKey = builder.Configuration[\"Jwt:Key\"] ?? throw new InvalidOperationException(\"JWT Key not configured\");
var jwtIssuer = builder.Configuration[\"Jwt:Issuer\"] ?? throw new InvalidOperationException(\"JWT Issuer not configured\");
var jwtAudience = builder.Configuration[\"Jwt:Audience\"] ?? throw new InvalidOperationException(\"JWT Audience not configured\");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // No clock skew tolerance for security
        };
    });

// CORS Policy with environment-specific origins
var corsOrigins = builder.Configuration.GetSection(\"Cors:AllowedOrigins\").Get<string[]>() ?? new[] { \"https://yourdomain.com\" };
builder.Services.AddCors(options =>
{
    options.AddPolicy(\"AllowConfiguredOrigins\", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Rate Limiting Configuration
builder.Services.AddMemoryCache();
builder.Services.AddInMemoryRateLimiting();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection(\"IpRateLimiting\"));
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyValueProcessingStrategy>();

var app = builder.Build();

// Configure the HTTP request pipeline

// Exception handling middleware (must be first)
app.UseMiddleware<ExceptionHandlerMiddleware>();

// HTTPS enforcement in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts(); // Strict-Transport-Security for 1 year
}

// Swagger UI only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

// CORS must be before authentication
app.UseCors(\"AllowConfiguredOrigins\");

// Rate limiting
app.UseIpRateLimiting();

// Authentication & Authorization middleware order is critical
app.UseAuthentication(); // Must be before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
