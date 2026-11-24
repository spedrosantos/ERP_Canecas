using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Identidade.DTOs.Usuarios;
using MS_Identidade.Services.Interfaces;

namespace MS_Identidade.Controllers;

[ApiController]
[Authorize]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpPost]
    [Authorize(Policy = "USUARIO_CADASTRAR")]
    public async Task<IActionResult> Cadastrar(
        CadastrarUsuarioDTO dto)
    {
        var empresaIdClaim = User.FindFirst("empresa_id")?.Value;

        if (!long.TryParse(empresaIdClaim, out var empresaId)
            || empresaId <= 0)
        {
            return Unauthorized();
        }

        var usuario = await _usuarioService
            .CadastrarAsync(empresaId, dto);

        return StatusCode(StatusCodes.Status201Created, usuario);
    }
}