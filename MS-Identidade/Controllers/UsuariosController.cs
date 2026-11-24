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

    [HttpGet]
    [Authorize(Policy = "USUARIO")]
    public async Task<ActionResult<List<UsuarioResponseDTO>>> Listar()
    {
        if (!TentarObterEmpresaId(out var empresaId))
        {
            return Unauthorized();
        }

        var usuarios = await _usuarioService.ListarAsync(empresaId);

        return Ok(usuarios);
    }

    [HttpPost]
    [Authorize(Policy = "USUARIO_CADASTRAR")]
    public async Task<IActionResult> Cadastrar(
        CadastrarUsuarioDTO dto)
    {
        if (!TentarObterEmpresaId(out var empresaId))
        {
            return Unauthorized();
        }

        var usuario = await _usuarioService
            .CadastrarAsync(empresaId, dto);

        return StatusCode(StatusCodes.Status201Created, usuario);
    }

    private bool TentarObterEmpresaId(out long empresaId)
    {
        var empresaIdClaim = User.FindFirst("empresa_id")?.Value;

        return long.TryParse(empresaIdClaim, out empresaId) && empresaId > 0;
    }
}
