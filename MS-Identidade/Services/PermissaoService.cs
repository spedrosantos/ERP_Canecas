using MS_Identidade.Data.Interfaces;
using MS_Identidade.Repositories;
using MS_Identidade.Repositories.Interfaces;
using MS_Identidade.Services.Interfaces;

namespace MS_Identidade.Services;

public class PermissaoService : IPermissaoService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly ITenantContextFactory _contextFactory;

    public PermissaoService(
        IEmpresaRepository empresaRepository,
        ITenantContextFactory contextFactory)
    {
        _empresaRepository = empresaRepository;
        _contextFactory = contextFactory;
    }

    public async Task<bool> PossuiPermissaoAsync(
        long empresaId,
        long usuarioId,
        string codigoFeature)
    {
        var empresa = await _empresaRepository
            .ObterPorIdAsync(empresaId);

        if (empresa == null || !empresa.Ativo)
            return false;

        await using var context =
            _contextFactory.Criar(empresa.NomeBanco);

        var usuarioRepository = new UsuarioRepository(context);

        var usuario = await usuarioRepository
            .ObterPorIdAsync(usuarioId);

        if (usuario == null || !usuario.Ativo)
            return false;

        var permissaoRepository = new PermissaoRepository(context);

        var features = await permissaoRepository
            .ListarPorUsuarioAsync(usuarioId);

        var feature = features.FirstOrDefault(
            x => x.Codigo == codigoFeature);

        if (feature == null)
            return false;

        var visitadas = new HashSet<long>();

        while (feature != null)
        {
            if (!visitadas.Add(feature.Id))
                return false;

            if (feature.IdFeaturePai == null)
                return true;

            feature = features.FirstOrDefault(
                x => x.Id == feature.IdFeaturePai);
        }

        return false;
    }
}