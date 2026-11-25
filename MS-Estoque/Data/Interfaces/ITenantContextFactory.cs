namespace MS_Estoque.Data.Interfaces;

public interface ITenantContextFactory
{
    Task<TenantDbContext> CriarAsync(CancellationToken cancellationToken = default);
}
