using Microsoft.EntityFrameworkCore;
using NiceCleanLib.Data;
using NiceCleanLib.Services.Interfaces;
using NiceCleanLib.Services.Repositories;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("NiceClean");
builder.Services.AddDbContext<NiceCleanDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Reposistories for database:
//builder.Services.AddScoped<IPinRepository, PinRepositoryDB>();
//builder.Services.AddScoped<IUserRepository, UserRepositoryDB>();
//builder.Services.AddScoped<IPinVoteRepository, PinVoteRepository>();

// Reposistories for testing with a collection in-memory:
builder.Services.AddSingleton<IPinRepository, PinRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IPinVoteRepository, PinVoteRepository>();

//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
//}

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
