namespace MultiTenancy.Interfaces;

public interface ITenantConnectionResolver
{
    string ObterConnectionString(string nomeBanco);
}