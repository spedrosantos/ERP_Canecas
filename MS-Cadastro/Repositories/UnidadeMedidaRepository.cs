using Microsoft.EntityFrameworkCore;
using MS_Cadastro.Data;
using MS_Cadastro.Entities;
using MS_Cadastro.Repositories.Interfaces;

namespace MS_Cadastro.Repositories;

public class UnidadeMedidaRepository : IUnidadeMedidaRepository
{
    private readonly TenantDbContext _context;

    public UnidadeMedidaRepository(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<List<UnidadeMedida>> ListarAsync(
        bool? ativo = null)
    {
        var query = _context.UnidadesMedida.AsNoTracking();

        if (ativo.HasValue)
            query = query.Where(x => x.Ativo == ativo.Value);

        return await query
            .OrderBy(x => x.Sigla)
            .ToListAsync();
    }

    public async Task<UnidadeMedida?> ObterPorIdAsync(long id)
    {
        return await _context.UnidadesMedida
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> SiglaExisteAsync(
        string sigla, long? ignorarId = null)
    {
        return await _context.UnidadesMedida.AnyAsync(x =>
            x.Sigla == sigla &&
            (!ignorarId.HasValue || x.Id != ignorarId.Value));
    }

    public void Adicionar(UnidadeMedida unidade)
    {
        _context.UnidadesMedida.Add(unidade);
    }

    public async Task SalvarAsync()
    {
        await _context.SaveChangesAsync();
    }
}