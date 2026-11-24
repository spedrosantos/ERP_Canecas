using MS_Identidade.Entities;

namespace MS_Identidade.Repositories.Interfaces;

public interface IPermissaoRepository
{
    Task<List<Feature>> ListarPorUsuarioAsync(long usuarioId);

    Task<ILookup<long, Feature>> ListarPorUsuariosAtivosAsync();

    Task<List<Feature>> ListarFeaturesAtivasAsync();

    Task<List<Feature>> ListarAtribuidasAsync(long usuarioId);

    Task AtualizarVinculosAsync(long usuarioId, IEnumerable<long> incluir, IEnumerable<long> remover);
}
