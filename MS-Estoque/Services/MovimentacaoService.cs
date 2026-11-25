using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MS_Estoque.Data.Interfaces;
using MS_Estoque.DTOs.Movimentacoes;
using MS_Estoque.Entities;
using MS_Estoque.Repositories;
using MS_Estoque.Rules;
using MS_Estoque.Services.Interfaces;

namespace MS_Estoque.Services;

public class MovimentacaoService(
    ITenantContextFactory contextFactory,
    IHttpContextAccessor httpContextAccessor) : IMovimentacaoService
{
    public async Task<MovimentacaoResponseDTO> AjustarAsync(
        AjustarEstoqueDTO dto,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioId();

        if (string.IsNullOrWhiteSpace(dto.Justificativa))
            throw new ValidationException(
                "Informe a justificativa do ajuste.");

        await using var context =
            await contextFactory.CriarAsync(cancellationToken);

        await using var transacao =
            await context.Database.BeginTransactionAsync(cancellationToken);

        var produtos = await context.Database.SqlQuery<long>($"""
            SELECT id AS "Value"
            FROM cadastro.produtos
            WHERE id = {dto.ProdutoId}
              AND ativo = TRUE
            FOR SHARE
            """).ToListAsync(cancellationToken);

        if (produtos.Count == 0)
            throw new ValidationException(
                "Produto inexistente ou inativo.");

        var repository = new MovimentacaoRepository(context);

        var saldo = await repository.BloquearSaldoAsync(
            dto.ProdutoId, cancellationToken);

        decimal custoUnitario;

        switch (dto.Tipo)
        {
            case "ENTRADA":
                custoUnitario = dto.CustoUnitario
                    ?? throw new ValidationException(
                        "Informe o custo unitário da entrada.");

                CalculoEstoque.RegistrarEntrada(
                    saldo, dto.Quantidade, custoUnitario);
                break;

            case "SAIDA":
                custoUnitario = CalculoEstoque.RegistrarSaida(
                    saldo, dto.Quantidade);
                break;

            default:
                throw new ValidationException(
                    "Tipo de movimentação inválido.");
        }

        var movimentacao = new MovimentacaoEstoque
        {
            ProdutoId = dto.ProdutoId,
            Tipo = dto.Tipo,
            Quantidade = dto.Quantidade,
            CustoUnitario = custoUnitario,
            Origem = "AJUSTE",
            UsuarioId = usuarioId,
            Justificativa = dto.Justificativa.Trim(),
            CriadoEm = DateTime.UtcNow
        };

        repository.Adicionar(movimentacao);

        await repository.SalvarAsync(cancellationToken);

        await transacao.CommitAsync(cancellationToken);

        return Mapear(movimentacao);
    }

    public async Task<MovimentacaoResponseDTO> EstornarAsync(
    long movimentacaoId,
    EstornarMovimentacaoDTO dto,
    CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioId();

        if (string.IsNullOrWhiteSpace(dto.Justificativa))
            throw new ValidationException(
                "Informe a justificativa do estorno.");

        await using var context =
            await contextFactory.CriarAsync(cancellationToken);

        await using var transacao =
            await context.Database.BeginTransactionAsync(
                cancellationToken);

        var repository = new MovimentacaoRepository(context);

        var original = await repository.ObterPorIdAsync(
            movimentacaoId, cancellationToken);

        if (original is null)
            throw new ValidationException(
                "Movimentação não encontrada.");

        if (original.Origem != "AJUSTE")
            throw new ValidationException(
                "Somente ajustes manuais podem ser estornados.");

        var saldo = await repository.BloquearSaldoAsync(
            original.ProdutoId, cancellationToken);

        if (await repository.PossuiEstornoAsync(
            original.Id, cancellationToken))
        {
            throw new ValidationException(
                "Esta movimentação já foi estornada.");
        }

        var custoUnitario = CalculoEstoque.RegistrarEstorno(
            saldo, original);

        var estorno = new MovimentacaoEstoque
        {
            ProdutoId = original.ProdutoId,
            Tipo = original.Tipo == "ENTRADA"
                ? "SAIDA"
                : "ENTRADA",
            Quantidade = original.Quantidade,
            CustoUnitario = custoUnitario,
            Origem = "ESTORNO",
            MovimentacaoOriginalId = original.Id,
            UsuarioId = usuarioId,
            Justificativa = dto.Justificativa.Trim(),
            CriadoEm = DateTime.UtcNow
        };

        repository.Adicionar(estorno);

        await repository.SalvarAsync(cancellationToken);

        await transacao.CommitAsync(cancellationToken);

        return Mapear(estorno);
    }

    private long ObterUsuarioId()
    {
        var valor = httpContextAccessor.HttpContext?
            .User.FindFirst("usuario_id")?.Value;

        if (!long.TryParse(valor, out var id) || id <= 0)
            throw new UnauthorizedAccessException(
                "Usuário não identificado.");

        return id;
    }

    private static MovimentacaoResponseDTO Mapear(
        MovimentacaoEstoque movimentacao)
    {
        return new MovimentacaoResponseDTO(
            movimentacao.Id,
            movimentacao.ProdutoId,
            movimentacao.Tipo,
            movimentacao.Quantidade,
            movimentacao.CustoUnitario,
            movimentacao.Origem,
            movimentacao.ReferenciaId,
            movimentacao.MovimentacaoOriginalId,
            movimentacao.UsuarioId,
            movimentacao.Justificativa,
            movimentacao.CriadoEm
        );
    }
    public async Task<MovimentacoesPaginadasResponseDTO> ListarAsync(
    ListarMovimentacoesDTO filtro,
    CancellationToken cancellationToken = default)
    {
        if (filtro.DataInicio.HasValue &&
            filtro.DataFim.HasValue &&
            filtro.DataInicio.Value > filtro.DataFim.Value)
        {
            throw new ValidationException(
                "A data inicial não pode ser maior que a final.");
        }

        if (filtro.ProdutoId.HasValue && filtro.ProdutoId.Value <= 0)
            throw new ValidationException("Código do produto inválido.");

        await using var context =
            await contextFactory.CriarAsync(cancellationToken);

        var repository = new MovimentacaoRepository(context);

        var (itens, total) = await repository.ListarAsync(
            filtro, cancellationToken);

        return new MovimentacoesPaginadasResponseDTO(
            itens.Select(Mapear).ToList(),
            total,
            filtro.Pagina,
            filtro.TamanhoPagina
        );
    }
}
