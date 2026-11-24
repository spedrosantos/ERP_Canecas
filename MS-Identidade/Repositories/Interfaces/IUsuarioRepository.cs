using MS_Identidade.Entities;

namespace MS_Identidade.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario> CadastrarAsync(Usuario usuario);

    Task<Usuario?> ObterPorIdAsync(long id);

    Task<Usuario?> ObterParaAtualizacaoAsync(long id);

    Task SalvarAlteracoesAsync();

    Task<List<Usuario>> ListarAsync();

    Task<List<Usuario>> ListarAtivosAsync();

    Task<Dictionary<long, bool>> ListarSituacoesAsync(IReadOnlyCollection<long> ids);
}
