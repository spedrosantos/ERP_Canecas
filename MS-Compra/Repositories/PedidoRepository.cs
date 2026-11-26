using Microsoft.EntityFrameworkCore;
using MS_Compra.Data;
using MS_Compra.DTOs.Pedidos;
using MS_Compra.Entities;
using MS_Compra.Repositories.Interfaces;

namespace MS_Compra.Repositories;

public class PedidoRepository(TenantDbContext context) : IPedidoRepository
{
    private static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public Task<PaginadoResponseDTO<PedidoResponseDTO>> ListarAsync(ListarPedidosDTO filtro, CancellationToken ct)
    {
        var consulta = context.Pedidos.AsNoTracking();
        if (filtro.FornecedorId is long fornecedorId)
            consulta = consulta.Where(x => x.FornecedorId == fornecedorId);
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
            x.Id, x.FornecedorId, x.UsuarioId, x.Situacao, x.ValorTotal, x.Observacao,
            x.CriadoEm, x.AtualizadoEm, x.RecebidoEm));

        return PaginarAsync(pedidos, filtro.Pagina, filtro.TamanhoPagina, ct);
    }

    public async Task<DashboardCompraDTO> ObterDashboardAsync(DateOnly inicio, DateOnly fim, CancellationToken ct)
    {
        var de = InicioDoDiaUtc(inicio);
        var ate = fim == DateOnly.MaxValue ? DateTime.MaxValue : InicioDoDiaUtc(fim.AddDays(1));
        var meses = await context.Database.SqlQuery<CompraMensalDTO>($"""
            SELECT date_trunc('month', recebido_em AT TIME ZONE 'America/Sao_Paulo')::date AS "Mes",
                SUM(valor_total) AS "Valor"
            FROM compras.pedidos
            WHERE situacao = {Pedido.Recebido} AND recebido_em >= {de} AND recebido_em < {ate}
            GROUP BY 1 ORDER BY 1
            """).ToListAsync(ct);
        return new(meses.Sum(x => x.Valor), meses);
    }

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
            SELECT * FROM compras.pedidos WHERE id = {id} FOR UPDATE
            """).ToListAsync(ct);

        var pedido = pedidos.SingleOrDefault();
        if (pedido is not null)
            await context.Entry(pedido).Collection(x => x.Itens).LoadAsync(ct);

        return pedido;
    }

    public async Task<bool> BloquearFornecedorAtivoAsync(long id, CancellationToken ct)
    {
        var fornecedores = await context.Database.SqlQuery<long>($"""
            SELECT id AS "Value" FROM cadastro.fornecedores
            WHERE id = {id} AND ativo FOR SHARE
            """).ToListAsync(ct);

        return fornecedores.Count == 1;
    }

    public Task<string?> ObterNomeFornecedorAsync(long id, CancellationToken ct) =>
        context.Database.SqlQuery<string>($"""
            SELECT nome AS "Value" FROM cadastro.fornecedores WHERE id = {id}
            """).SingleOrDefaultAsync(ct);

    public async Task<int> BloquearProdutosAtivosAsync(long[] ids, CancellationToken ct)
    {
        var produtos = await context.Database.SqlQuery<long>($"""
            SELECT id AS "Value" FROM cadastro.produtos
            WHERE id = ANY ({ids}) AND ativo ORDER BY id FOR SHARE
            """).ToListAsync(ct);

        return produtos.Count;
    }

    public Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarFornecedoresAsync(
        ListarCadastrosDTO filtro, CancellationToken ct) =>
        ListarCadastrosAsync(context.Database.SqlQuery<CadastroAtivoResponseDTO>($"""
            SELECT id AS "Id", nome AS "Nome", NULL::text AS "UnidadeMedida"
            FROM cadastro.fornecedores WHERE ativo
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
