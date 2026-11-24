using MS_Identidade.Entities;

namespace MS_Identidade.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario> CadastrarAsync(Usuario usuario);

    Task<Usuario?> ObterPorIdAsync(long id);

    Task<List<Usuario>> ListarAsync();
}