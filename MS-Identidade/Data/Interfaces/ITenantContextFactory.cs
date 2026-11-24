namespace MS_Identidade.Data.Interfaces;

public interface ITenantContextFactory
{
    TenantDbContext Criar(string nomeBanco);
}