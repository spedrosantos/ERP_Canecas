using Microsoft.EntityFrameworkCore;
using MS_Identidade.Data;
using MultiTenancy.Interfaces;
using MultiTenancy.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("MasterConnection")
    ?? throw new InvalidOperationException(
        "Connection string MasterConnection não configurada.");

builder.Services.AddDbContext<MasterDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<
ITenantConnectionResolver,
TenantConnectionResolver>();


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
