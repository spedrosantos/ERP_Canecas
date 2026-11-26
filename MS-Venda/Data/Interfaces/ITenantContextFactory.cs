namespace MS_Venda.Data.Interfaces;

public interface ITenantContextFactory
{
    Task<TenantDbContext> CriarAsync(CancellationToken cancellationToken = default);
}
