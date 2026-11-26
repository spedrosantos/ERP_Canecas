using MS_Estoque.DTOs.Saldos;

namespace MS_Estoque.Services.Interfaces;

public interface ISaldoService
{
    Task<SaldosPaginadosResponseDTO> ListarAsync(
        ListarSaldosDTO filtro,
        CancellationToken cancellationToken = default);

    Task ConfigurarMinimoAsync(
        long produtoId,
        ConfigurarEstoqueMinimoDTO dto,
        CancellationToken cancellationToken = default);
}
