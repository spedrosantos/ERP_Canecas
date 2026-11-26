using MS_Estoque.Entities;
using MS_Estoque.DTOs.Movimentacoes;

namespace MS_Estoque.Repositories.Interfaces;

public interface IMovimentacaoRepository
{
    Task<(List<MovimentacaoResponseDTO> Itens, long Total)> ListarAsync(
        ListarMovimentacoesDTO filtro,
        CancellationToken cancellationToken = default);

    Task<Saldo> BloquearSaldoAsync(
        long produtoId,
        CancellationToken cancellationToken = default);

    Task<MovimentacaoEstoque?> ObterPorIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<bool> PossuiEstornoAsync(
        long movimentacaoId,
        CancellationToken cancellationToken = default);

    void Adicionar(MovimentacaoEstoque movimentacao);

    Task SalvarAsync(
        CancellationToken cancellationToken = default);
}
