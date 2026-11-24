using Microsoft.EntityFrameworkCore;
using MS_Identidade.Data.Interfaces;
using MultiTenancy.Interfaces;

namespace MS_Identidade.Data;

public class TenantContextFactory : ITenantContextFactory
{
    private readonly ITenantConnectionResolver _resolver;

    public TenantContextFactory(ITenantConnectionResolver resolver)
    {
        _resolver = resolver;
    }

    public TenantDbContext Criar(string nomeBanco)
    {
        var connectionString = _resolver
            .ObterConnectionString(nomeBanco);

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TenantDbContext(options);
    }
}