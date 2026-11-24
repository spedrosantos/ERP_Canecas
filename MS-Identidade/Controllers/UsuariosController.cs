using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Identidade.DTOs.Usuarios;
using MS_Identidade.Services;
using MS_Identidade.Services.Interfaces;

namespace MS_Identidade.Controllers;

[ApiController]
[Authorize]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly IPermissaoService _permissaoService;

    public UsuariosController(IUsuarioService usuarioService, IPermissaoService permissaoService)
    {
        _usuarioService = usuarioService;
        _permissaoService = permissaoService;
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

    [HttpGet("permissoes/features")]
    [Authorize(Policy = "USUARIO_PERMISSOES")]
    public async Task<ActionResult<List<FeatureResponseDTO>>> ListarFeatures()
    {
        if (!TentarObterEmpresaId(out var empresaId))
            return Unauthorized();

        return Ok(await _permissaoService.ListarFeaturesAsync(empresaId));
    }

    [HttpGet("{id:long}/permissoes")]
    [Authorize(Policy = "USUARIO_PERMISSOES")]
    public async Task<ActionResult<List<FeatureResponseDTO>>> ListarPermissoes(
        [Range(1, long.MaxValue)] long id)
    {
        if (!TentarObterEmpresaId(out var empresaId))
            return Unauthorized();

        var features = await _permissaoService.ListarAtribuidasAsync(empresaId, id);

        return features is null ? NotFound() : Ok(features);
    }

    [HttpPut("{id:long}/permissoes")]
    [Authorize(Policy = "USUARIO_PERMISSOES")]
    public async Task<IActionResult> AtualizarPermissoes(
        [Range(1, long.MaxValue)] long id, AtualizarPermissoesDTO dto)
    {
        if (!TentarObterEmpresaId(out var empresaId)
            || !TentarObterId("usuario_id", out var operadorId))
            return Unauthorized();

        if (dto.FeatureIds is null)
            return BadRequest(new { mensagem = "Informe a lista de IDs das features." });

        var resultado = await _permissaoService.AtualizarAsync(
            empresaId, operadorId, id, dto.FeatureIds);

        return resultado switch
        {
            ResultadoAtualizacaoPermissoes.Atualizado => NoContent(),
            ResultadoAtualizacaoPermissoes.UsuarioInexistente => NotFound(),
            ResultadoAtualizacaoPermissoes.AlteracaoPropria =>
                Conflict(new { mensagem = "Você não pode alterar as próprias permissões." }),
            ResultadoAtualizacaoPermissoes.SemAutoridade => Forbid(),
            ResultadoAtualizacaoPermissoes.FeaturesInvalidas =>
                BadRequest(new { mensagem = "Informe IDs distintos de features ativas." }),
            ResultadoAtualizacaoPermissoes.HierarquiaInvalida =>
                BadRequest(new { mensagem = "As features selecionadas exigem uma hierarquia ativa e completa." }),
            ResultadoAtualizacaoPermissoes.UltimoGerenciador =>
                Conflict(new { mensagem = "A empresa deve manter ao menos um usuário ativo com acesso efetivo ao gerenciamento de permissões." }),
            ResultadoAtualizacaoPermissoes.AlteracaoSimultanea =>
                Conflict(new { mensagem = "As permissões ou a situação dos usuários foram alteradas simultaneamente. Tente novamente." }),
            _ => throw new InvalidOperationException("Resultado de atualização desconhecido.")
        };
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
