using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Estoque.Authorization;
using MS_Estoque.DTOs.Movimentacoes;
using MS_Estoque.Services.Interfaces;

namespace MS_Estoque.Controllers;

[ApiController]
[Route("api/estoque/movimentacoes")]
[Authorize(Policy = FeaturesEstoque.Estoque)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class MovimentacoesController(
    IMovimentacaoService movimentacaoService) : ControllerBase
{
    [HttpPost("ajuste")]
    public async Task<ActionResult<MovimentacaoResponseDTO>> Ajustar(
        AjustarEstoqueDTO dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var movimentacao =
                await movimentacaoService.AjustarAsync(
                    dto, cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created, movimentacao);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
    [HttpGet]
    public async Task<ActionResult<MovimentacoesPaginadasResponseDTO>> Listar(
    [FromQuery] ListarMovimentacoesDTO filtro,
    CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await movimentacaoService.ListarAsync(
                filtro, cancellationToken));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }
    [HttpPost("{id:long}/estorno")]
    public async Task<ActionResult<MovimentacaoResponseDTO>> Estornar(
    long id,
    EstornarMovimentacaoDTO dto,
    CancellationToken cancellationToken)
    {
        try
        {
            var estorno = await movimentacaoService.EstornarAsync(
                id, dto, cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created, estorno);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
