using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MS_Estoque.Data;
using MS_Estoque.Data.Interfaces;
using MS_Estoque.Authorization;
using FeatureAuthorization;
using MultiTenancy.Interfaces;
using MultiTenancy.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var masterConnection = builder.Configuration.GetConnectionString("MasterConnection")
    ?? throw new InvalidOperationException("Connection string MasterConnection não configurada.");

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(masterConnection));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantConnectionResolver, TenantConnectionResolver>();
builder.Services.AddScoped<ITenantContextFactory, TenantContextFactory>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Chave JWT não configurada.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
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

builder.Services.AdicionarAutorizacaoPorFeatures(
    builder.Configuration.GetSection("Integracoes:Identidade"),
    builder.Environment.IsDevelopment());

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(FeaturesEstoque.Estoque, policy =>
    {
        policy.AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme);

        policy.RequireAuthenticatedUser();

        policy.AddRequirements(new FeatureRemotaRequirement(
            FeaturesEstoque.Estoque,
            JwtBearerDefaults.AuthenticationScheme));
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
