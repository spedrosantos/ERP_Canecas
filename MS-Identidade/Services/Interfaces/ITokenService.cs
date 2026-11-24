using MS_Identidade.Entities;
using MS_Identidade.DTOs.Auth;

namespace MS_Identidade.Services.Interfaces;

public interface ITokenService
{
    LoginResponseDTO GerarToken(
        Usuario usuario,
        long empresaId);
}