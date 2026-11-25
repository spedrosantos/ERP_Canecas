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
}
