using MS_Estoque.DTOs.Movimentacoes;

namespace MS_Estoque.Services.Interfaces;

public interface IMovimentacaoService
{
    Task<MovimentacoesPaginadasResponseDTO> ListarAsync(
        ListarMovimentacoesDTO filtro,
        CancellationToken cancellationToken = default);

    Task<MovimentacaoResponseDTO> AjustarAsync(
        AjustarEstoqueDTO dto,
        CancellationToken cancellationToken = default);
}
