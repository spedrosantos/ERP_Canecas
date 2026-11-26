using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MS_Compra.Data;
using MS_Compra.Data.Interfaces;
using MS_Compra.Authorization;
using FeatureAuthorization;
using MultiTenancy.Interfaces;
using MultiTenancy.Services;
using Npgsql;
using MS_Compra.Services;
using MS_Compra.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var masterConnection = builder.Configuration.GetConnectionString("MasterConnection")
    ?? throw new InvalidOperationException("Connection string MasterConnection não configurada.");

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(masterConnection));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantConnectionResolver, TenantConnectionResolver>();
builder.Services.AddScoped<ITenantContextFactory, TenantContextFactory>();
builder.Services.AddScoped<IPedidoService, PedidoService>();

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
    options.AddPolicy(FeaturesCompra.Compra, policy =>
    {
        policy.AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme);

        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            long.TryParse(context.User.FindFirst("empresa_id")?.Value, out var empresaId) && empresaId > 0 &&
            long.TryParse(context.User.FindFirst("usuario_id")?.Value, out var usuarioId) && usuarioId > 0);

        policy.AddRequirements(new FeatureRemotaRequirement(
            FeaturesCompra.Compra,
            JwtBearerDefaults.AuthenticationScheme));
    });
});

builder.Services.AddControllers().ConfigureApplicationPartManager(manager =>
{
    // A referência ao Estoque reutiliza a persistência e as regras de entrada.
    var estoque = manager.ApplicationParts.FirstOrDefault(part =>
        part.Name == typeof(MS_Estoque.Data.TenantDbContext).Assembly.GetName().Name);
    if (estoque is not null)
        manager.ApplicationParts.Remove(estoque);
});
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
