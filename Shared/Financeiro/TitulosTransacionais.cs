using Microsoft.EntityFrameworkCore;

namespace Financeiro;

public sealed class TitulosTransacionais(DbContext contexto)
{
    private static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateOnly DataLocal(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, Fuso));

    public void GerarParaCompra(long id, decimal valor, DateOnly? vencimento, long usuarioId, DateTime criadoEm)
    {
        if (valor > 0)
            Gerar(Titulo.Pagar, id, null, valor, vencimento, usuarioId, criadoEm);
    }

    public void GerarParaVenda(long id, decimal valor, DateOnly? vencimento, long usuarioId, DateTime criadoEm) =>
        Gerar(Titulo.Receber, null, id, valor, vencimento, usuarioId, criadoEm);

    private void Gerar(string tipo, long? compraId, long? vendaId, decimal valor,
        DateOnly? vencimento, long usuarioId, DateTime criadoEm)
    {
        ExigirTransacao();
        contexto.Set<Titulo>().Add(new Titulo
        {
            Tipo = tipo, CompraId = compraId, VendaId = vendaId, Valor = valor,
            Vencimento = vencimento ?? DataLocal(criadoEm), UsuarioId = usuarioId, CriadoEm = criadoEm
        });
    }

    public async Task CancelarDaVendaAsync(long vendaId, DateTime canceladoEm, CancellationToken ct)
    {
        var titulo = await BloquearAsync($"""
            SELECT * FROM financeiro.titulos WHERE venda_id = {vendaId} FOR UPDATE
            """, ct);

        // Vendas anteriores à integração podem não ter título.
        if (titulo is null)
            return;

        if (await BaixasAtivas(titulo.Id).AnyAsync(ct))
            throw new ConflitoFinanceiroException("Estorne as baixas financeiras antes de cancelar a venda.");

        titulo.CanceladoEm ??= canceladoEm;
    }

    public Task<Titulo?> BloquearPorIdAsync(long id, CancellationToken ct) => BloquearAsync($"""
        SELECT * FROM financeiro.titulos WHERE id = {id} FOR UPDATE
        """, ct);

    public IQueryable<Baixa> BaixasAtivas(long tituloId) =>
        contexto.Set<Baixa>().Where(x => x.TituloId == tituloId && x.EstornadoEm == null);

    private async Task<Titulo?> BloquearAsync(FormattableString sql, CancellationToken ct)
    {
        ExigirTransacao();
        var titulos = await contexto.Set<Titulo>().FromSqlInterpolated(sql).ToListAsync(ct);
        return titulos.SingleOrDefault();
    }

    private void ExigirTransacao()
    {
        if (contexto.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A operação financeira exige uma transação.");
    }
}

public sealed class ConflitoFinanceiroException(string mensagem) : Exception(mensagem);
