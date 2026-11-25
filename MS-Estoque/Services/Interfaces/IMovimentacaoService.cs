using MS_Estoque.DTOs.Movimentacoes;

namespace MS_Estoque.Services.Interfaces;

public interface IMovimentacaoService
{
    Task<MovimentacaoResponseDTO> AjustarAsync(
        AjustarEstoqueDTO dto,
        CancellationToken cancellationToken = default);
}
