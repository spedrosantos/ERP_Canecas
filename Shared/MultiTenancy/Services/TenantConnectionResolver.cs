using Microsoft.Extensions.Configuration;
using MultiTenancy.Interfaces;

namespace MultiTenancy.Services;

public class TenantConnectionResolver : ITenantConnectionResolver
{
    private readonly IConfiguration _configuration;

    public TenantConnectionResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string ObterConnectionString(string nomeBanco)
    {
        var connectionString = _configuration
            .GetConnectionString(nomeBanco);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Conexão da empresa não configurada.");
        }

        return connectionString;
    }
}