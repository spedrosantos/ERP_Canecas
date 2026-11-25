using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MS_Estoque.Data.Interfaces;
using MS_Estoque.DTOs.Saldos;
using MS_Estoque.Repositories;
using MS_Estoque.Services.Interfaces;

namespace MS_Estoque.Services;

public class SaldoService(
    ITenantContextFactory contextFactory) : ISaldoService
{
    public async Task<SaldosPaginadosResponseDTO> ListarAsync(
        ListarSaldosDTO filtro,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await contextFactory.CriarAsync(cancellationToken);

        var repository = new SaldoRepository(context);

        return await repository.ListarAsync(
            filtro, cancellationToken);
    }

    public async Task ConfigurarMinimoAsync(
        long produtoId,
        ConfigurarEstoqueMinimoDTO dto,
        CancellationToken cancellationToken = default)
    {
        var minimo = dto.EstoqueMinimo
            ?? throw new ValidationException(
                "Informe o estoque mínimo.");

        if (minimo < 0 ||
            minimo > 999999999999.999m ||
            decimal.Round(minimo, 3) != minimo)
        {
            throw new ValidationException(
                "Estoque mínimo inválido.");
        }

        await using var context =
            await contextFactory.CriarAsync(cancellationToken);

        await using var transacao =
            await context.Database.BeginTransactionAsync(
                cancellationToken);

        var produtos = await context.Database.SqlQuery<long>($"""
            SELECT id AS "Value"
            FROM cadastro.produtos
            WHERE id = {produtoId}
              AND ativo = TRUE
            FOR SHARE
            """).ToListAsync(cancellationToken);

        if (produtos.Count == 0)
            throw new ValidationException(
                "Produto inexistente ou inativo.");

        var repository = new MovimentacaoRepository(context);

        var saldo = await repository.BloquearSaldoAsync(
            produtoId, cancellationToken);

        saldo.EstoqueMinimo = minimo;
        saldo.AtualizadoEm = DateTime.UtcNow;

        await repository.SalvarAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
    }
}
