using Microsoft.EntityFrameworkCore;
using MS_Identidade.Data;
using MultiTenancy.Interfaces;
using MultiTenancy.Services;
using MS_Identidade.Repositories;
using MS_Identidade.Repositories.Interfaces;
using MS_Identidade.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using MS_Identidade.Entities;
using MS_Identidade.Services;
using MS_Identidade.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

builder.Services.AddScoped<IUsuarioService, UsuarioService>();

builder.Services.AddScoped<
    IPasswordHasher<Usuario>,
    PasswordHasher<Usuario>>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<ITokenService, TokenService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Chave JWT não configurada.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPermissaoService, PermissaoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
