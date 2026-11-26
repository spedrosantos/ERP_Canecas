using MS_Estoque.DTOs.Saldos;

namespace MS_Estoque.Repositories.Interfaces;

public interface ISaldoRepository
{
    Task<SaldosPaginadosResponseDTO> ListarAsync(
        ListarSaldosDTO filtro,
        CancellationToken cancellationToken = default);
}
