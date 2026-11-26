using Financeiro;
using MS_Financeiro.DTOs;

namespace MS_Financeiro.Repositories.Interfaces;

public interface ITituloRepository
{
    Task<TitulosPaginadosResponseDTO> ListarAsync(ListarTitulosDTO filtro, DateOnly hoje, CancellationToken ct);
    Task<TituloResponseDTO?> ObterAsync(long id, DateOnly hoje, CancellationToken ct);
    Task<List<Baixa>> ListarBaixasAsync(long tituloId, CancellationToken ct);
    Task<Baixa?> ObterBaixaAsync(long tituloId, long baixaId, CancellationToken ct);
    void Adicionar(Baixa baixa);
    Task<IndicadoresFinanceirosResponseDTO> ObterIndicadoresAsync(FiltroTitulosDTO filtro, DateOnly hoje, CancellationToken ct);
}
