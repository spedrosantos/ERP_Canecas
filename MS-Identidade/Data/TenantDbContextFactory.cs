using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MultiTenancy.Services;

namespace MS_Identidade.Data;

public class TenantDbContextFactory
    : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        if (args.Length != 1)
        {
            throw new InvalidOperationException(
                "Informe o nome do banco da empresa.");
        }

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TenantDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var resolver = new TenantConnectionResolver(configuration);

        var connectionString = resolver
            .ObterConnectionString(args[0]);

        var options = new DbContextOptionsBuilder<TenantDbContext>();

        options.UseNpgsql(connectionString);

        return new TenantDbContext(options.Options);
    }
}