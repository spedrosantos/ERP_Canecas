namespace MS_Financeiro.Data.Interfaces;

public interface ITenantContextFactory
{
    Task<TenantDbContext> CriarAsync(CancellationToken cancellationToken = default);
}
