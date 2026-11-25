using MS_Cadastro.DTOs.Produtos;

namespace MS_Cadastro.Services.Interfaces;

public interface IProdutoService
{
    Task<ProdutosPaginadosResponseDTO> ListarAsync(
        ListarProdutosDTO filtro);

    Task<ProdutoResponseDTO?> ObterPorIdAsync(
        long id);

    Task<ProdutoResponseDTO> CadastrarAsync(
        SalvarProdutoDTO dto);

    Task<ProdutoResponseDTO?> AtualizarAsync(
        long id, SalvarProdutoDTO dto);

    Task<bool> AlterarSituacaoAsync(
        long id, bool ativo);
}