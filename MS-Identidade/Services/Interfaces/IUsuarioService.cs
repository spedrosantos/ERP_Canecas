using MS_Identidade.DTOs.Usuarios;

namespace MS_Identidade.Services.Interfaces;

public interface IUsuarioService
{
    Task<UsuarioResponseDTO> CadastrarAsync(
        long empresaId,
        CadastrarUsuarioDTO dto);
}