using MS_Identidade.Entities;

namespace MS_Identidade.Repositories.Interfaces;

public interface IPermissaoRepository
{
    Task<List<Feature>> ListarPorUsuarioAsync(long usuarioId);
}