using MS_Cadastro.DTOs.Fornecedores;
using MS_Cadastro.Entities;

namespace MS_Cadastro.Repositories.Interfaces;

public interface IFornecedorRepository
{
    Task<(List<Fornecedor> Itens, long Total)> ListarAsync(ListarFornecedoresDTO filtro, CancellationToken cancellationToken);
    Task<Fornecedor?> ObterPorIdAsync(long id, CancellationToken cancellationToken);
    Task<Fornecedor?> ObterParaAtualizacaoAsync(long id, CancellationToken cancellationToken);
    void Adicionar(Fornecedor fornecedor);
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
