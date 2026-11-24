using Microsoft.EntityFrameworkCore;
using MS_Identidade.Data;
using MS_Identidade.Entities;
using MS_Identidade.Repositories.Interfaces;

namespace MS_Identidade.Repositories;

public class PermissaoRepository : IPermissaoRepository
{
    private readonly TenantDbContext _context;

    public PermissaoRepository(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<List<Feature>> ListarPorUsuarioAsync(long usuarioId)
    {
        return await (
            from uf in _context.UsuariosFeatures
            join f in _context.Features
                on uf.FeatureId equals f.Id
            where uf.UsuarioId == usuarioId
                && f.Ativo
            select f
        )
        .AsNoTracking()
        .ToListAsync();
    }

    public async Task<ILookup<long, Feature>> ListarPorUsuariosAtivosAsync()
    {
        var permissoes = await (
            from uf in _context.UsuariosFeatures
            join usuario in _context.Usuarios on uf.UsuarioId equals usuario.Id
            join feature in _context.Features on uf.FeatureId equals feature.Id
            where usuario.Ativo && feature.Ativo
            select new { uf.UsuarioId, Feature = feature }
        )
        .AsNoTracking()
        .ToListAsync();

        return permissoes.ToLookup(x => x.UsuarioId, x => x.Feature);
    }
}
