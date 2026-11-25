using Microsoft.EntityFrameworkCore;
using MS_Estoque.Data;
using MS_Estoque.DTOs.Saldos;
using MS_Estoque.Repositories.Interfaces;

namespace MS_Estoque.Repositories;

public class SaldoRepository(
    TenantDbContext context) : ISaldoRepository
{
    public async Task<SaldosPaginadosResponseDTO> ListarAsync(
        ListarSaldosDTO filtro,
        CancellationToken cancellationToken = default)
    {
        var consulta = context.Database.SqlQuery<SaldoResponseDTO>($"""
            SELECT
                p.id AS "ProdutoId",
                p.nome AS "NomeProduto",
                u.sigla AS "UnidadeMedida",
                COALESCE(s.quantidade, 0) AS "Quantidade",
                COALESCE(s.estoque_minimo, 0) AS "EstoqueMinimo",
                COALESCE(s.custo_medio, 0) AS "CustoMedio",
                s.atualizado_em AS "AtualizadoEm"
            FROM cadastro.produtos p
            INNER JOIN cadastro.unidades_medida u
                ON u.id = p.unidade_medida_id
            LEFT JOIN estoque.saldos s
                ON s.produto_id = p.id
            WHERE p.ativo = TRUE
                OR COALESCE(s.quantidade, 0) > 0
            """);

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var busca = filtro.Busca.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");

            var termo = $"%{busca}%";

            var codigoValido = long.TryParse(
                filtro.Busca.Trim(), out var codigo);

            consulta = consulta.Where(x =>
                EF.Functions.ILike(x.NomeProduto, termo, "\\") ||
                (codigoValido && x.ProdutoId == codigo));
        }

        if (filtro.AbaixoDoMinimo.HasValue)
        {
            consulta = consulta.Where(x =>
                (x.Quantidade < x.EstoqueMinimo)
                    == filtro.AbaixoDoMinimo.Value);
        }

        var total = await consulta.LongCountAsync(cancellationToken);

        var deslocamento =
            (long)(filtro.Pagina - 1) * filtro.TamanhoPagina;

        if (deslocamento >= total || deslocamento > int.MaxValue)
        {
            return new SaldosPaginadosResponseDTO(
                [], total, filtro.Pagina, filtro.TamanhoPagina);
        }

        var itens = await consulta
            .OrderBy(x => x.ProdutoId)
            .Skip((int)deslocamento)
            .Take(filtro.TamanhoPagina)
            .ToListAsync(cancellationToken);

        return new SaldosPaginadosResponseDTO(
            itens, total, filtro.Pagina, filtro.TamanhoPagina);
    }
}
