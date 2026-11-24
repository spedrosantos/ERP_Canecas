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

    [HttpPut("{id:long}")]
    [Authorize(Policy = "USUARIO_EDITAR")]
    public async Task<ActionResult<UsuarioResponseDTO>> Editar(
        long id, EditarUsuarioDTO dto)
    {
        if (!TentarObterEmpresaId(out var empresaId))
            return Unauthorized();

        var usuario = await _usuarioService.EditarAsync(empresaId, id, dto);

        return usuario is null ? NotFound() : Ok(usuario);
    }

    [HttpPatch("{id:long}/situacao")]
    [Authorize(Policy = "USUARIO_EDITAR")]
    public async Task<ActionResult<UsuarioResponseDTO>> AlterarSituacao(
        long id, AlterarSituacaoUsuarioDTO dto)
    {
        if (!TentarObterEmpresaId(out var empresaId)
            || !TentarObterId("usuario_id", out var usuarioAutenticadoId))
            return Unauthorized();

        var (usuario, conflito) = await _usuarioService.AlterarSituacaoAsync(
            empresaId, usuarioAutenticadoId, id, dto.Ativo.GetValueOrDefault());

        if (conflito is not null)
            return Conflict(new { mensagem = conflito });

        return usuario is null ? NotFound() : Ok(usuario);
    }

    private bool TentarObterEmpresaId(out long empresaId)
    {
        return TentarObterId("empresa_id", out empresaId);
    }

    private bool TentarObterId(string claim, out long id)
    {
        return long.TryParse(User.FindFirst(claim)?.Value, out id) && id > 0;
    }
}
