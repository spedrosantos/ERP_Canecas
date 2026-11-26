using Microsoft.EntityFrameworkCore;
using MS_Venda.Data.Interfaces;
using MultiTenancy.Interfaces;
using Npgsql;

namespace MS_Venda.Data;

public class TenantContextFactory : ITenantContextFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NpgsqlDataSource _master;
    private readonly ITenantConnectionResolver _resolver;

    public TenantContextFactory(
        IHttpContextAccessor httpContextAccessor,
        NpgsqlDataSource master,
        ITenantConnectionResolver resolver)
    {
        _httpContextAccessor = httpContextAccessor;
        _master = master;
        _resolver = resolver;
    }

    public async Task<TenantDbContext> CriarAsync(CancellationToken cancellationToken = default)
    {
        var usuario = _httpContextAccessor.HttpContext?.User;
        if (usuario?.Identity?.IsAuthenticated != true
            || !long.TryParse(usuario.FindFirst("empresa_id")?.Value, out var empresaId)
            || empresaId <= 0)
        {
            throw new UnauthorizedAccessException("Empresa não identificada no JWT.");
        }

        await using var comando = _master.CreateCommand(
            "SELECT nome_banco FROM empresas WHERE id = @empresa_id AND ativo");
        comando.Parameters.AddWithValue("empresa_id", empresaId);

        var nomeBanco = await comando.ExecuteScalarAsync(cancellationToken) as string;
        if (string.IsNullOrWhiteSpace(nomeBanco))
            throw new InvalidOperationException("Empresa inexistente ou inativa.");

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(_resolver.ObterConnectionString(nomeBanco))
            .Options;

        return new TenantDbContext(options);
    }
}
