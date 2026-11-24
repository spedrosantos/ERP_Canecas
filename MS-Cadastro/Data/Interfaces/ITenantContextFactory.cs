namespace MS_Cadastro.Data.Interfaces;

public interface ITenantContextFactory
{
    Task<TenantDbContext> CriarAsync(CancellationToken cancellationToken = default);
}
