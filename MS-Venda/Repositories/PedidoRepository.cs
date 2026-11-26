using System.Data;
using Microsoft.EntityFrameworkCore;
using MS_Venda.Data;
using MS_Venda.DTOs.Pedidos;
using MS_Venda.Entities;
using MS_Venda.Repositories.Interfaces;
using MS_Estoque.Entities;

namespace MS_Venda.Repositories;

public class PedidoRepository(TenantDbContext context) : IPedidoRepository
{
    private static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public Task<PaginadoResponseDTO<PedidoResponseDTO>> ListarAsync(ListarPedidosDTO filtro, CancellationToken ct)
    {
        var consulta = context.Pedidos.AsNoTracking();
        if (filtro.ClienteId is long clienteId)
            consulta = consulta.Where(x => x.ClienteId == clienteId);
        if (filtro.Situacao is not null)
            consulta = consulta.Where(x => x.Situacao == filtro.Situacao);

        if (filtro.DataInicio is DateOnly inicio)
        {
            var limite = InicioDoDiaUtc(inicio);
            consulta = consulta.Where(x => x.CriadoEm >= limite);
        }

        if (filtro.DataFim is DateOnly fim && fim < DateOnly.MaxValue)
        {
            var limite = InicioDoDiaUtc(fim.AddDays(1));
            consulta = consulta.Where(x => x.CriadoEm < limite);
        }

        var pedidos = consulta.OrderBy(x => x.Id).Select(x => new PedidoResponseDTO(
            x.Id, x.ClienteId, x.UsuarioId, x.Situacao, x.ValorTotal, x.Observacao,
            x.CriadoEm, x.AtualizadoEm, x.ConfirmadoEm, x.CanceladoEm));

        return PaginarAsync(pedidos, filtro.Pagina, filtro.TamanhoPagina, ct);
    }

    public async Task<DashboardVendaDTO> ObterDashboardAsync(DateOnly inicio, DateOnly fim, CancellationToken ct)
    {
        var de = InicioDoDiaUtc(inicio);
        var ate = fim == DateOnly.MaxValue ? DateTime.MaxValue : InicioDoDiaUtc(fim.AddDays(1));
        await using var leitura = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);

        var meses = await context.Database.SqlQuery<VendaMensalDTO>($"""
            SELECT date_trunc('month', confirmado_em AT TIME ZONE 'America/Sao_Paulo')::date AS "Mes",
                SUM(valor_total) AS "Valor", COUNT(*) AS "Quantidade"
            FROM vendas.pedidos
            WHERE situacao = {Pedido.Confirmado} AND confirmado_em >= {de} AND confirmado_em < {ate}
            GROUP BY 1 ORDER BY 1
            """).ToListAsync(ct);

        var custo = await context.Database.SqlQuery<CustoDashboard>($"""
            SELECT COALESCE(SUM(i.quantidade * i.custo_unitario), 0) AS "Valor",
                COALESCE(BOOL_OR(i.custo_unitario IS NULL), FALSE) AS "Incompleto"
            FROM vendas.pedidos p
            LEFT JOIN vendas.pedidos_itens i ON i.pedido_id = p.id
            WHERE p.situacao = {Pedido.Confirmado} AND p.confirmado_em >= {de} AND p.confirmado_em < {ate}
            """).SingleAsync(ct);

        var ranking = await context.Database.SqlQuery<ProdutoMaisVendidoDTO>($"""
            SELECT i.produto_id AS "ProdutoId", pr.nome AS "NomeProduto", SUM(i.quantidade) AS "Quantidade"
            FROM vendas.pedidos p
            JOIN vendas.pedidos_itens i ON i.pedido_id = p.id
            LEFT JOIN cadastro.produtos pr ON pr.id = i.produto_id
            WHERE p.situacao = {Pedido.Confirmado} AND p.confirmado_em >= {de} AND p.confirmado_em < {ate}
            GROUP BY i.produto_id, pr.nome
            ORDER BY SUM(i.quantidade) DESC, i.produto_id
            LIMIT 5
            """).ToListAsync(ct);

        var ultimas = await context.Database.SqlQuery<VendaRecenteDTO>($"""
            SELECT p.id AS "Id", p.cliente_id AS "ClienteId", c.nome AS "NomeCliente",
                p.confirmado_em AS "ConfirmadoEm", p.valor_total AS "Valor"
            FROM vendas.pedidos p
            LEFT JOIN cadastro.clientes c ON c.id = p.cliente_id
            WHERE p.situacao = {Pedido.Confirmado} AND p.confirmado_em >= {de} AND p.confirmado_em < {ate}
            ORDER BY p.confirmado_em DESC, p.id DESC
            LIMIT 5
            """).ToListAsync(ct);

        await leitura.CommitAsync(ct);
        var faturamento = meses.Sum(x => x.Valor);
        decimal? lucro = custo.Incompleto ? null : faturamento - custo.Valor;
        decimal? margem = faturamento > 0 && lucro.HasValue
            ? decimal.Round(lucro.Value / faturamento * 100, 2, MidpointRounding.AwayFromZero)
            : null;
        return new(faturamento, meses.Sum(x => x.Quantidade),
            lucro.HasValue ? decimal.Round(lucro.Value, 2, MidpointRounding.AwayFromZero) : null,
            margem, meses, ranking, ultimas);
    }

    private sealed record CustoDashboard(decimal Valor, bool Incompleto);

    private static DateTime InicioDoDiaUtc(DateOnly data)
    {
        var meiaNoite = data.ToDateTime(TimeOnly.MinValue);

        // No início do horário de verão, a meia-noite local pode ser suprimida.
        if (Fuso.IsInvalidTime(meiaNoite))
            return new DateTimeOffset(meiaNoite, Fuso.GetUtcOffset(meiaNoite.AddHours(-1))).UtcDateTime;

        return TimeZoneInfo.ConvertTimeToUtc(meiaNoite, Fuso);
    }

    public Task<Pedido?> ObterAsync(long id, CancellationToken ct) =>
        context.Pedidos.AsNoTracking().Include(x => x.Itens).SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Pedido?> BloquearAsync(long id, CancellationToken ct)
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("O bloqueio do pedido exige uma transação.");

        var pedidos = await context.Pedidos.FromSqlInterpolated($"""
            SELECT * FROM vendas.pedidos WHERE id = {id} FOR UPDATE
            """).ToListAsync(ct);

        var pedido = pedidos.SingleOrDefault();
        if (pedido is not null)
            await context.Entry(pedido).Collection(x => x.Itens).LoadAsync(ct);

        return pedido;
    }

    public async Task<bool> BloquearClienteAtivoAsync(long id, CancellationToken ct)
    {
        var clientes = await context.Database.SqlQuery<long>($"""
            SELECT id AS "Value" FROM cadastro.clientes
            WHERE id = {id} AND ativo FOR SHARE
            """).ToListAsync(ct);

        return clientes.Count == 1;
    }

    public Task<string?> ObterNomeClienteAsync(long id, CancellationToken ct) =>
        context.Database.SqlQuery<string>($"""
            SELECT nome AS "Value" FROM cadastro.clientes WHERE id = {id}
            """).SingleOrDefaultAsync(ct);

    public async Task<int> BloquearProdutosAtivosAsync(long[] ids, CancellationToken ct)
    {
        var produtos = await context.Database.SqlQuery<long>($"""
            SELECT id AS "Value" FROM cadastro.produtos
            WHERE id = ANY ({ids}) AND ativo ORDER BY id FOR SHARE
            """).ToListAsync(ct);

        return produtos.Count;
    }

    public Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarClientesAsync(
        ListarCadastrosDTO filtro, CancellationToken ct) =>
        ListarCadastrosAsync(context.Database.SqlQuery<CadastroAtivoResponseDTO>($"""
            SELECT id AS "Id", nome AS "Nome", NULL::text AS "UnidadeMedida"
            FROM cadastro.clientes WHERE ativo
            """), filtro, ct);

    public Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarProdutosAsync(
        ListarCadastrosDTO filtro, CancellationToken ct) =>
        ListarCadastrosAsync(context.Database.SqlQuery<CadastroAtivoResponseDTO>($"""
            SELECT p.id AS "Id", p.nome AS "Nome", u.sigla AS "UnidadeMedida"
            FROM cadastro.produtos p
            JOIN cadastro.unidades_medida u ON u.id = p.unidade_medida_id
            WHERE p.ativo
            """), filtro, ct);

    public Task<Dictionary<long, CadastroAtivoResponseDTO>> ObterProdutosAsync(long[] ids, CancellationToken ct) =>
        context.Database.SqlQuery<CadastroAtivoResponseDTO>($"""
            SELECT p.id AS "Id", p.nome AS "Nome", u.sigla AS "UnidadeMedida"
            FROM cadastro.produtos p
            JOIN cadastro.unidades_medida u ON u.id = p.unidade_medida_id
            WHERE p.id = ANY ({ids})
            """).ToDictionaryAsync(x => x.Id, ct);

    public Task<DisponibilidadeProdutoResponseDTO?> ObterDisponibilidadeAsync(long produtoId, CancellationToken ct) =>
        context.Database.SqlQuery<DisponibilidadeProdutoResponseDTO>($"""
            SELECT p.id AS "ProdutoId", p.nome AS "NomeProduto", u.sigla AS "UnidadeMedida",
                COALESCE(s.quantidade, 0) AS "QuantidadeDisponivel"
            FROM cadastro.produtos p
            JOIN cadastro.unidades_medida u ON u.id = p.unidade_medida_id
            LEFT JOIN estoque.saldos s ON s.produto_id = p.id
            WHERE p.id = {produtoId} AND p.ativo
            """).SingleOrDefaultAsync(ct);

    public Task<List<MovimentacaoEstoque>> ListarSaidasDaVendaAsync(long pedidoId, CancellationToken ct) =>
        context.Movimentacoes.AsNoTracking()
            .Where(x => x.ReferenciaId == pedidoId && x.Origem == "VENDA" && x.Tipo == "SAIDA")
            .OrderBy(x => x.ProdutoId).ToListAsync(ct);

    public void Adicionar(Pedido pedido) => context.Pedidos.Add(pedido);

    public void RemoverItem(PedidoItem item) => context.Itens.Remove(item);

    private static Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarCadastrosAsync(
        IQueryable<CadastroAtivoResponseDTO> consulta, ListarCadastrosDTO filtro, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var busca = filtro.Busca.Trim();
            var texto = busca.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var termo = $"%{texto}%";
            var temCodigo = long.TryParse(busca, out var codigo);
            consulta = consulta.Where(x => EF.Functions.ILike(x.Nome, termo, "\\") || (temCodigo && x.Id == codigo));
        }

        return PaginarAsync(consulta.OrderBy(x => x.Id),
            filtro.Pagina, filtro.TamanhoPagina, ct);
    }

    private static async Task<PaginadoResponseDTO<T>> PaginarAsync<T>(
        IQueryable<T> consulta, int pagina, int tamanho, CancellationToken ct)
    {
        var total = await consulta.LongCountAsync(ct);
        var deslocamento = (pagina - 1L) * tamanho;
        IReadOnlyList<T> itens = [];
        if (deslocamento < total && deslocamento <= int.MaxValue)
            itens = await consulta.Skip((int)deslocamento).Take(tamanho).ToListAsync(ct);

        return new(itens, total, pagina, tamanho);
    }
}
