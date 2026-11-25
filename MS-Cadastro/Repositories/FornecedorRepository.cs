using Microsoft.EntityFrameworkCore;
using MS_Cadastro.Data;
using MS_Cadastro.DTOs.Fornecedores;
using MS_Cadastro.Entities;
using MS_Cadastro.Repositories.Interfaces;

namespace MS_Cadastro.Repositories;

public class FornecedorRepository(TenantDbContext context) : IFornecedorRepository
{
    public async Task<(List<Fornecedor> Itens, long Total)> ListarAsync(
        ListarFornecedoresDTO filtro, CancellationToken cancellationToken)
    {
        var consulta = context.Fornecedores.AsNoTracking();

        if (filtro.Ativo is bool ativo)
            consulta = consulta.Where(fornecedor => fornecedor.Ativo == ativo);

        if (!string.IsNullOrEmpty(filtro.TipoPessoa))
        {
            var tipo = filtro.TipoPessoa.ToUpperInvariant();
            consulta = consulta.Where(fornecedor => fornecedor.TipoPessoa == tipo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var busca = filtro.Busca.Trim();
            var documento = busca.Replace(".", "").Replace("-", "").Replace("/", "");
            var termo = PadraoBuscaParcial(busca);
            var termoDocumento = PadraoBuscaParcial(documento.Length == 0 ? busca : documento);
            consulta = consulta.Where(fornecedor =>
                EF.Functions.ILike(fornecedor.Nome, termo, "\\")
                || (fornecedor.NomeFantasia != null && EF.Functions.ILike(fornecedor.NomeFantasia, termo, "\\"))
                || EF.Functions.ILike(fornecedor.CpfCnpj, termoDocumento, "\\"));
        }

        var total = await consulta.LongCountAsync(cancellationToken);
        var deslocamento = ((long)filtro.Pagina - 1) * filtro.TamanhoPagina;
        if (deslocamento >= total)
            return ([], total);

        var itens = await consulta.OrderBy(fornecedor => fornecedor.Nome).ThenBy(fornecedor => fornecedor.Id)
            .Skip(checked((int)deslocamento))
            .Take(filtro.TamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public Task<Fornecedor?> ObterPorIdAsync(long id, CancellationToken cancellationToken)
    {
        return context.Fornecedores.AsNoTracking().FirstOrDefaultAsync(fornecedor => fornecedor.Id == id, cancellationToken);
    }

    public Task<Fornecedor?> ObterParaAtualizacaoAsync(long id, CancellationToken cancellationToken)
    {
        return context.Fornecedores.FirstOrDefaultAsync(fornecedor => fornecedor.Id == id, cancellationToken);
    }

    public void Adicionar(Fornecedor fornecedor)
    {
        context.Fornecedores.Add(fornecedor);
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
