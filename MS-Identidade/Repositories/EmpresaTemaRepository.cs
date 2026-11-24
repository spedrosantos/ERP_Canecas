using Microsoft.EntityFrameworkCore;
using MS_Identidade.Data;
using MS_Identidade.Entities;
using MS_Identidade.Repositories.Interfaces;

namespace MS_Identidade.Repositories;

public class EmpresaTemaRepository : IEmpresaTemaRepository
{
    private readonly MasterDbContext _context;

    public EmpresaTemaRepository(MasterDbContext context)
    {
        _context = context;
    }

    public async Task<EmpresaTema?> ObterPorEmpresaIdAsync(long empresaId)
    {
        return await _context.EmpresasTemas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId);
    }
}
