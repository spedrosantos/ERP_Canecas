using Microsoft.EntityFrameworkCore;
using MS_Estoque.Data;
using MS_Estoque.Entities;
using MS_Estoque.Repositories.Interfaces;

namespace MS_Estoque.Repositories;

public class MovimentacaoRepository(
    TenantDbContext context) : IMovimentacaoRepository
{
    public async Task<Saldo> BloquearSaldoAsync(
        long produtoId,
        CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "O bloqueio do saldo exige uma transação.");

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO estoque.saldos (produto_id)
            VALUES ({produtoId})
            ON CONFLICT (produto_id) DO NOTHING
            """, cancellationToken);

        var saldos = await context.Saldos
            .FromSqlInterpolated($"""
                SELECT *
                FROM estoque.saldos
                WHERE produto_id = {produtoId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);

        return saldos.Single();
    }

    public async Task<MovimentacaoEstoque?> ObterPorIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await context.Movimentacoes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id, cancellationToken);
    }

    public async Task<bool> PossuiEstornoAsync(
        long movimentacaoId,
        CancellationToken cancellationToken = default)
    {
        return await context.Movimentacoes.AnyAsync(
            x => x.MovimentacaoOriginalId == movimentacaoId,
            cancellationToken);
    }

    public void Adicionar(MovimentacaoEstoque movimentacao)
    {
        context.Movimentacoes.Add(movimentacao);
    }

    public async Task SalvarAsync(
        CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
