using MS_Identidade.DTOs.Usuarios;

namespace MS_Identidade.Services.Interfaces;

public interface IUsuarioService
{
    Task<List<UsuarioResponseDTO>> ListarAsync(long empresaId);

    Task<UsuarioResponseDTO?> EditarAsync(
        long empresaId, long id, EditarUsuarioDTO dto);

    Task<UsuarioResponseDTO?> AlterarSenhaAsync(long empresaId, long id, string senha);

    Task<(UsuarioResponseDTO? Usuario, string? Conflito)> AlterarSituacaoAsync(
        long empresaId, long usuarioAutenticadoId, long id, bool ativo);

    Task<UsuarioResponseDTO> CadastrarAsync(
        long empresaId,
        CadastrarUsuarioDTO dto);
}
