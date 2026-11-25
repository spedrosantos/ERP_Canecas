using Microsoft.EntityFrameworkCore;
using MS_Cadastro.Data;
using MS_Cadastro.DTOs.Clientes;
using MS_Cadastro.Entities;
using MS_Cadastro.Repositories.Interfaces;

namespace MS_Cadastro.Repositories;

public class ClienteRepository(TenantDbContext context) : IClienteRepository
{
    public async Task<(List<Cliente> Itens, long Total)> ListarAsync(
        ListarClientesDTO filtro, CancellationToken cancellationToken)
    {
        var consulta = context.Clientes.AsNoTracking();

        if (filtro.Ativo is bool ativo)
            consulta = consulta.Where(cliente => cliente.Ativo == ativo);

        if (!string.IsNullOrEmpty(filtro.TipoPessoa))
        {
            var tipo = filtro.TipoPessoa.ToUpperInvariant();
            consulta = consulta.Where(cliente => cliente.TipoPessoa == tipo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var busca = filtro.Busca.Trim();
            var documento = busca.Replace(".", "").Replace("-", "").Replace("/", "");
            var termo = PadraoBuscaParcial(busca);
            var termoDocumento = PadraoBuscaParcial(documento.Length == 0 ? busca : documento);
            consulta = consulta.Where(cliente =>
                EF.Functions.ILike(cliente.Nome, termo, "\\")
                || (cliente.NomeFantasia != null && EF.Functions.ILike(cliente.NomeFantasia, termo, "\\"))
                || EF.Functions.ILike(cliente.CpfCnpj, termoDocumento, "\\"));
        }

        var total = await consulta.LongCountAsync(cancellationToken);
        var deslocamento = ((long)filtro.Pagina - 1) * filtro.TamanhoPagina;
        if (deslocamento >= total)
            return ([], total);

        var itens = await consulta.OrderBy(cliente => cliente.Id)
            .Skip(checked((int)deslocamento))
            .Take(filtro.TamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public Task<Cliente?> ObterPorIdAsync(long id, CancellationToken cancellationToken)
    {
        return context.Clientes.AsNoTracking().FirstOrDefaultAsync(cliente => cliente.Id == id, cancellationToken);
    }

    public Task<Cliente?> ObterParaAtualizacaoAsync(long id, CancellationToken cancellationToken)
    {
        return context.Clientes.FirstOrDefaultAsync(cliente => cliente.Id == id, cancellationToken);
    }

    public void Adicionar(Cliente cliente)
    {
        context.Clientes.Add(cliente);
    }

    public async Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string PadraoBuscaParcial(string termo)
    {
        return "%" + termo.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    }
}
