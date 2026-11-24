using Microsoft.EntityFrameworkCore;
using MS_Identidade.Data;
using MS_Identidade.Entities;
using MS_Identidade.Repositories.Interfaces;

namespace MS_Identidade.Repositories;

public class EmpresaRepository : IEmpresaRepository
{
    private readonly MasterDbContext _context;

    public EmpresaRepository(MasterDbContext context)
    {
        _context = context;
    }

    public async Task<Empresa?> ObterPorIdAsync(long id)
    {
        return await _context.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}