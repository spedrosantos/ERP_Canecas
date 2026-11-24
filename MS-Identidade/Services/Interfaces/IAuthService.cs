using MS_Identidade.DTOs.Auth;
using MS_Identidade.Entities;

namespace MS_Identidade.Services.Interfaces;

public interface IAuthService
{
    Task<Usuario?> ValidarCredenciaisAsync(LoginDTO dto);

    Task<bool> UsuarioAtivoAsync(long empresaId, long usuarioId);
}
