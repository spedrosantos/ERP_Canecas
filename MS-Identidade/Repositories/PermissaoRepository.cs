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
            join usuario in _context.Usuarios on uf.UsuarioId equals usuario.Id
            where uf.UsuarioId == usuarioId && usuario.Ativo && f.Ativo
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

    public Task<List<Feature>> ListarFeaturesAtivasAsync()
    {
        return _context.Features.AsNoTracking()
            .Where(feature => feature.Ativo)
            .OrderBy(feature => feature.Nome)
            .ThenBy(feature => feature.Id)
            .ToListAsync();
    }

    public Task<List<Feature>> ListarAtribuidasAsync(long usuarioId)
    {
        return (
            from vinculo in _context.UsuariosFeatures
            join feature in _context.Features on vinculo.FeatureId equals feature.Id
            where vinculo.UsuarioId == usuarioId
            orderby feature.Nome, feature.Id
            select feature
        ).AsNoTracking().ToListAsync();
    }

    public Task<List<Feature>> ListarCatalogoAsync()
    {
        return _context.Features.AsNoTracking().ToListAsync();
    }

    public async Task<ILookup<long, Feature>> ListarAtribuidasAsync(IReadOnlyCollection<long> usuarioIds)
    {
        var permissoes = await (
            from vinculo in _context.UsuariosFeatures
            join feature in _context.Features on vinculo.FeatureId equals feature.Id
            where usuarioIds.Contains(vinculo.UsuarioId)
            select new { vinculo.UsuarioId, Feature = feature }
        ).AsNoTracking().ToListAsync();

        return permissoes.ToLookup(x => x.UsuarioId, x => x.Feature);
    }

    public Task AtualizarVinculosAsync(
        long usuarioId, IEnumerable<long> incluir, IEnumerable<long> remover)
    {
        return AtualizarVinculosAsync(
            incluir.Select(id => new UsuarioFeature { UsuarioId = usuarioId, FeatureId = id }),
            remover.Select(id => new UsuarioFeature { UsuarioId = usuarioId, FeatureId = id }));
    }

    public async Task AtualizarVinculosAsync(
        IEnumerable<UsuarioFeature> incluir, IEnumerable<UsuarioFeature> remover)
    {
        _context.UsuariosFeatures.RemoveRange(remover);
        _context.UsuariosFeatures.AddRange(incluir);

        await _context.SaveChangesAsync();
    }
}
