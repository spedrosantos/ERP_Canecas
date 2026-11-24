using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MS_Cadastro.Data;
using MS_Cadastro.Data.Interfaces;
using MS_Cadastro.Authorization;
using MS_Cadastro.Configuration;
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AdicionarIntegracoesHttp(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddScoped<IAuthorizationHandler, ConsultaCadastralAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ConsultaCadastralRequirement.CodigoFeature, policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new ConsultaCadastralRequirement());
    });
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
