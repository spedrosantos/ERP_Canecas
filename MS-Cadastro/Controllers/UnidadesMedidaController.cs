using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Cadastro.Authorization;
using MS_Cadastro.DTOs.UnidadesMedida;
using MS_Cadastro.DTOs.Produtos;
using MS_Cadastro.Services;
using MS_Cadastro.Services.Interfaces;

namespace MS_Cadastro.Controllers;

[ApiController]
[Route("api/unidades-medida")]
[Authorize(Policy = FeaturesCadastro.Produto)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class UnidadesMedidaController(
    IUnidadeMedidaService unidadeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UnidadeMedidaResponseDTO>>> Listar(
        [FromQuery] bool? ativo)
    {
        return Ok(await unidadeService.ListarAsync(ativo));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<UnidadeMedidaResponseDTO>> Obter(long id)
    {
        var unidade = await unidadeService.ObterPorIdAsync(id);

        return unidade is null ? NotFound() : Ok(unidade);
    }

    [HttpPost]
    public async Task<ActionResult<UnidadeMedidaResponseDTO>> Cadastrar(
        SalvarUnidadeMedidaDTO dto)
    {
        try
        {
            var unidade = await unidadeService.CadastrarAsync(dto);

            return CreatedAtAction(
                nameof(Obter),
                new { id = unidade.Id },
                unidade);
        }
        catch (CadastroDuplicadoException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<UnidadeMedidaResponseDTO>> Atualizar(
        long id, SalvarUnidadeMedidaDTO dto)
    {
        try
        {
            var unidade = await unidadeService.AtualizarAsync(id, dto);

            return unidade is null ? NotFound() : Ok(unidade);
        }
        catch (CadastroDuplicadoException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPatch("{id:long}/situacao")]
    public async Task<IActionResult> AlterarSituacao(
        long id, AlterarSituacaoProdutoDTO dto)
    {
        try
        {
            var alterado = await unidadeService.AlterarSituacaoAsync(
                id, dto.Ativo!.Value);

            return alterado ? NoContent() : NotFound();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }
}