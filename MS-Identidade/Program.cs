using Microsoft.EntityFrameworkCore;
using MS_Identidade.Data;
using MultiTenancy.Interfaces;
using MultiTenancy.Services;
using MS_Identidade.Repositories;
using MS_Identidade.Repositories.Interfaces;
using MS_Identidade.Data.Interfaces;

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

builder.Services.AddScoped<IEmpresaRepository, EmpresaRepository>();

builder.Services.AddScoped<
    ITenantContextFactory,
    TenantContextFactory>();

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
