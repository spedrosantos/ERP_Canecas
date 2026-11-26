using MS_Financeiro.DTOs;

namespace MS_Financeiro.Services.Interfaces;

public interface ITituloService
{
    Task<TitulosPaginadosResponseDTO> ListarAsync(ListarTitulosDTO filtro, CancellationToken ct);
    Task<TituloDetalheResponseDTO?> ObterAsync(long id, CancellationToken ct);
    Task<BaixaResponseDTO?> RegistrarBaixaAsync(long id, RegistrarBaixaDTO dto, CancellationToken ct);
    Task<BaixaResponseDTO?> EstornarBaixaAsync(long id, long baixaId, EstornarBaixaDTO dto, CancellationToken ct);
    Task<IndicadoresFinanceirosResponseDTO> ObterIndicadoresAsync(FiltroTitulosDTO filtro, CancellationToken ct);
}
