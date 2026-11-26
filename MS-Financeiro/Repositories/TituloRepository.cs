using Financeiro;
using Microsoft.EntityFrameworkCore;
using MS_Financeiro.Data;
using MS_Financeiro.DTOs;
using MS_Financeiro.Repositories.Interfaces;

namespace MS_Financeiro.Repositories;

public sealed class TituloRepository(TenantDbContext contexto) : ITituloRepository
{
    public async Task<TitulosPaginadosResponseDTO> ListarAsync(ListarTitulosDTO filtro, DateOnly hoje, CancellationToken ct)
    {
        var consulta = Filtrar(filtro, hoje);
        var total = await consulta.LongCountAsync(ct);
        var deslocamento = (filtro.Pagina - 1L) * filtro.TamanhoPagina;
        var itens = deslocamento < total && deslocamento <= int.MaxValue
            ? await Selecionar(consulta.OrderBy(x => x.Id).Skip((int)deslocamento).Take(filtro.TamanhoPagina), hoje)
                .ToListAsync(ct)
            : [];

        return new(itens, total, filtro.Pagina, filtro.TamanhoPagina);
    }

    public Task<TituloResponseDTO?> ObterAsync(long id, DateOnly hoje, CancellationToken ct) =>
        Selecionar(Consultar().Where(x => x.Id == id), hoje).SingleOrDefaultAsync(ct);

    public Task<List<Baixa>> ListarBaixasAsync(long tituloId, CancellationToken ct) =>
        contexto.Baixas.AsNoTracking().Where(x => x.TituloId == tituloId).OrderBy(x => x.Id).ToListAsync(ct);

    public Task<Baixa?> ObterBaixaAsync(long tituloId, long baixaId, CancellationToken ct) =>
        contexto.Baixas.SingleOrDefaultAsync(x => x.TituloId == tituloId && x.Id == baixaId, ct);

    public void Adicionar(Baixa baixa) => contexto.Baixas.Add(baixa);

    public async Task<IndicadoresFinanceirosResponseDTO> ObterIndicadoresAsync(
        FiltroTitulosDTO filtro, DateOnly hoje, CancellationToken ct)
    {
        var totais = await Filtrar(filtro, hoje).Where(x => x.CanceladoEm == null)
            .GroupBy(x => x.Tipo)
            .Select(grupo => new
            {
                Tipo = grupo.Key,
                Total = grupo.LongCount(),
                Valor = grupo.Sum(x => x.Valor),
                Baixado = grupo.Sum(x => x.ValorBaixado),
                Vencido = grupo.Sum(x => x.Vencimento < hoje ? x.Valor - x.ValorBaixado : 0m)
            })
            .ToDictionaryAsync(x => x.Tipo,
                x => new IndicadorFinanceiroDTO(x.Total, x.Valor, x.Baixado, x.Valor - x.Baixado, x.Vencido), ct);

        var vazio = new IndicadorFinanceiroDTO(0, 0, 0, 0, 0);
        return new(totais.GetValueOrDefault(Titulo.Pagar, vazio), totais.GetValueOrDefault(Titulo.Receber, vazio));
    }

    private IQueryable<TituloConsulta> Consultar() => contexto.Database.SqlQuery<TituloConsulta>($"""
        SELECT t.id AS "Id", t.tipo AS "Tipo", t.compra_id AS "CompraId", t.venda_id AS "VendaId",
            c.fornecedor_id AS "FornecedorId", f.nome AS "NomeFornecedor",
            v.cliente_id AS "ClienteId", cl.nome AS "NomeCliente", t.valor AS "Valor",
            COALESCE(b.valor, 0) AS "ValorBaixado", t.vencimento AS "Vencimento",
            t.usuario_id AS "UsuarioId", t.criado_em AS "CriadoEm", t.cancelado_em AS "CanceladoEm"
        FROM financeiro.titulos t
        LEFT JOIN compras.pedidos c ON c.id = t.compra_id
        LEFT JOIN cadastro.fornecedores f ON f.id = c.fornecedor_id
        LEFT JOIN vendas.pedidos v ON v.id = t.venda_id
        LEFT JOIN cadastro.clientes cl ON cl.id = v.cliente_id
        LEFT JOIN LATERAL (
            SELECT SUM(valor) AS valor
            FROM financeiro.baixas
            WHERE titulo_id = t.id AND estornado_em IS NULL
        ) b ON TRUE
        """);

    private IQueryable<TituloConsulta> Filtrar(FiltroTitulosDTO filtro, DateOnly hoje)
    {
        var consulta = Consultar();
        if (filtro.Tipo is not null)
            consulta = consulta.Where(x => x.Tipo == filtro.Tipo);
        if (filtro.FornecedorId is long fornecedor)
            consulta = consulta.Where(x => x.FornecedorId == fornecedor);
        if (filtro.ClienteId is long cliente)
            consulta = consulta.Where(x => x.ClienteId == cliente);
        if (filtro.VencimentoInicio is DateOnly inicio)
            consulta = consulta.Where(x => x.Vencimento >= inicio);
        if (filtro.VencimentoFim is DateOnly fim)
            consulta = consulta.Where(x => x.Vencimento <= fim);

        return filtro.Situacao switch
        {
            SituacaoTitulo.Cancelado => consulta.Where(x => x.CanceladoEm != null),
            SituacaoTitulo.Quitado => consulta.Where(x => x.CanceladoEm == null && x.ValorBaixado >= x.Valor),
            SituacaoTitulo.Vencido => consulta.Where(x => x.CanceladoEm == null &&
                x.ValorBaixado < x.Valor && x.Vencimento < hoje),
            SituacaoTitulo.Parcial => consulta.Where(x => x.CanceladoEm == null && x.ValorBaixado > 0 &&
                x.ValorBaixado < x.Valor && x.Vencimento >= hoje),
            SituacaoTitulo.Pendente => consulta.Where(x => x.CanceladoEm == null && x.ValorBaixado == 0 &&
                x.Valor > 0 && x.Vencimento >= hoje),
            _ => consulta
        };
    }

    private static IQueryable<TituloResponseDTO> Selecionar(IQueryable<TituloConsulta> consulta, DateOnly hoje) =>
        consulta.Select(x => new TituloResponseDTO(x.Id, x.Tipo, x.CompraId, x.VendaId,
            x.FornecedorId, x.NomeFornecedor, x.ClienteId, x.NomeCliente, x.Valor, x.ValorBaixado,
            x.Valor - x.ValorBaixado,
            x.CanceladoEm != null ? SituacaoTitulo.Cancelado :
                x.ValorBaixado >= x.Valor ? SituacaoTitulo.Quitado :
                x.Vencimento < hoje ? SituacaoTitulo.Vencido :
                x.ValorBaixado > 0 ? SituacaoTitulo.Parcial : SituacaoTitulo.Pendente,
            x.Vencimento, x.UsuarioId, x.CriadoEm, x.CanceladoEm));

    private sealed record TituloConsulta(long Id, string Tipo, long? CompraId, long? VendaId,
        long? FornecedorId, string? NomeFornecedor, long? ClienteId, string? NomeCliente,
        decimal Valor, decimal ValorBaixado, DateOnly Vencimento, long UsuarioId,
        DateTime CriadoEm, DateTime? CanceladoEm);
}
