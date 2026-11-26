namespace MS_Compra.Data.Interfaces;

public interface ITenantContextFactory
{
    Task<TenantDbContext> CriarAsync(CancellationToken cancellationToken = default);
}
