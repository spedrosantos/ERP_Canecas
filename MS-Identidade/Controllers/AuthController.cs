using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Identidade.DTOs.Auth;
using MS_Identidade.Services.Interfaces;

namespace MS_Identidade.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;

    public AuthController(
        IAuthService authService,
        ITokenService tokenService)
    {
        _authService = authService;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO dto)
    {
        var usuario = await _authService
            .ValidarCredenciaisAsync(dto);

        if (usuario is null)
        {
            return Unauthorized(new
            {
                mensagem = "Credenciais inválidas."
            });
        }

        var response = _tokenService
            .GerarToken(usuario, dto.EmpresaId);

        return Ok(response);
    }
}