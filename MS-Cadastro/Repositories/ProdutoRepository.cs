using Microsoft.EntityFrameworkCore;
using MS_Cadastro.Data;
using MS_Cadastro.Entities;
using MS_Cadastro.DTOs.Produtos;
using MS_Cadastro.Repositories.Interfaces;

namespace MS_Cadastro.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly TenantDbContext _context;

    public ProdutoRepository(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Produto> Itens, long Total)> ListarAsync(
        ListarProdutosDTO filtro)
    {
        var query = _context.Produtos
            .AsNoTracking()
            .Include(x => x.Categoria)
            .Include(x => x.UnidadeMedida)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var busca = filtro.Busca.Trim();
            var buscarPorId = long.TryParse(busca, out var id) && id > 0;
            var nome = busca
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");

            var termo = $"%{nome}%";

            query = query.Where(x =>
                EF.Functions.ILike(x.Nome, termo, "\\") ||
                (buscarPorId && x.Id == id));
        }

        if (filtro.CategoriaId.HasValue)
            query = query.Where(x =>
                x.CategoriaId == filtro.CategoriaId.Value);

        if (!string.IsNullOrWhiteSpace(filtro.Tipo))
            query = query.Where(x => x.Tipo == filtro.Tipo);

        if (filtro.Ativo.HasValue)
            query = query.Where(x =>
                x.Ativo == filtro.Ativo.Value);

        var total = await query.LongCountAsync();

        var deslocamento =
            (long)(filtro.Pagina - 1) * filtro.TamanhoPagina;

        if (deslocamento >= total || deslocamento > int.MaxValue)
            return (new List<Produto>(), total);

        var itens = await query
            .OrderBy(x => x.Nome)
            .ThenBy(x => x.Id)
            .Skip((int)deslocamento)
            .Take(filtro.TamanhoPagina)
            .ToListAsync();

        return (itens, total);
    }

    public async Task<Produto?> ObterPorIdAsync(long id)
    {
        return await _context.Produtos
            .Include(x => x.Categoria)
            .Include(x => x.UnidadeMedida)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public void Adicionar(Produto produto)
    {
        _context.Produtos.Add(produto);
    }

    public async Task SalvarAsync()
    {
        await _context.SaveChangesAsync();
    }
}