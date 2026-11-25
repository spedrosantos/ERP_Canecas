using Microsoft.EntityFrameworkCore;
using MS_Cadastro.Data;
using MS_Cadastro.Entities;
using MS_Cadastro.Repositories.Interfaces;

namespace MS_Cadastro.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly TenantDbContext _context;

    public CategoriaRepository(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<List<Categoria>> ListarAsync(bool? ativo = null)
    {
        var query = _context.Categorias.AsNoTracking();

        if (ativo.HasValue)
            query = query.Where(x => x.Ativo == ativo.Value);

        return await query
            .OrderBy(x => x.Nome)
            .ToListAsync();
    }

    public async Task<Categoria?> ObterPorIdAsync(long id)
    {
        return await _context.Categorias
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> NomeExisteAsync(
        string nome, long? ignorarId = null)
    {
        return await _context.Categorias.AnyAsync(x =>
            x.Nome == nome &&
            (!ignorarId.HasValue || x.Id != ignorarId.Value));
    }

    public void Adicionar(Categoria categoria)
    {
        _context.Categorias.Add(categoria);
    }

    public async Task SalvarAsync()
    {
        await _context.SaveChangesAsync();
    }
}