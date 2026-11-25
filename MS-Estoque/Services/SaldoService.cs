using MS_Estoque.Data.Interfaces;
using MS_Estoque.DTOs.Saldos;
using MS_Estoque.Repositories;
using MS_Estoque.Services.Interfaces;

namespace MS_Estoque.Services;

public class SaldoService(
    ITenantContextFactory contextFactory) : ISaldoService
{
    public async Task<SaldosPaginadosResponseDTO> ListarAsync(
        ListarSaldosDTO filtro,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await contextFactory.CriarAsync(cancellationToken);

        var repository = new SaldoRepository(context);

        return await repository.ListarAsync(
            filtro, cancellationToken);
    }
}
