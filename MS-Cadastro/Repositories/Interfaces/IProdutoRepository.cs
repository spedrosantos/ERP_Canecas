using MS_Cadastro.Entities;
using MS_Cadastro.DTOs.Produtos;

namespace MS_Cadastro.Repositories.Interfaces;

public interface IProdutoRepository
{
    Task<(List<Produto> Itens, long Total)> ListarAsync(
        ListarProdutosDTO filtro);

    Task<Produto?> ObterPorIdAsync(long id);

    void Adicionar(Produto produto);

    Task SalvarAsync();
}