using Microsoft.EntityFrameworkCore;
using MS_Estoque.Data;
using MS_Estoque.Entities;
using MS_Estoque.Repositories.Interfaces;
using MS_Estoque.DTOs.Movimentacoes;

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
    public async Task<(List<MovimentacaoEstoque> Itens, long Total)> ListarAsync(
    ListarMovimentacoesDTO filtro,
    CancellationToken cancellationToken = default)
    {
        var consulta = context.Movimentacoes
            .AsNoTracking()
            .AsQueryable();

        if (filtro.ProdutoId.HasValue)
            consulta = consulta.Where(x =>
                x.ProdutoId == filtro.ProdutoId.Value);

        if (!string.IsNullOrWhiteSpace(filtro.Tipo))
            consulta = consulta.Where(x => x.Tipo == filtro.Tipo);

        if (!string.IsNullOrWhiteSpace(filtro.Origem))
            consulta = consulta.Where(x => x.Origem == filtro.Origem);

        var fuso = TimeZoneInfo.FindSystemTimeZoneById(
            "America/Sao_Paulo");

        if (filtro.DataInicio.HasValue)
        {
            var inicio = TimeZoneInfo.ConvertTimeToUtc(
                filtro.DataInicio.Value.ToDateTime(TimeOnly.MinValue),
                fuso);

            consulta = consulta.Where(x => x.CriadoEm >= inicio);
        }

        if (filtro.DataFim.HasValue &&
            filtro.DataFim.Value < DateOnly.MaxValue)
        {
            var fim = TimeZoneInfo.ConvertTimeToUtc(
                filtro.DataFim.Value.AddDays(1)
                    .ToDateTime(TimeOnly.MinValue),
                fuso);

            consulta = consulta.Where(x => x.CriadoEm < fim);
        }

        var total = await consulta.LongCountAsync(cancellationToken);

        var deslocamento =
            (long)(filtro.Pagina - 1) * filtro.TamanhoPagina;

        if (deslocamento >= total || deslocamento > int.MaxValue)
            return ([], total);

        var itens = await consulta
            .OrderByDescending(x => x.Id)
            .Skip((int)deslocamento)
            .Take(filtro.TamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }
}
