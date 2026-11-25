using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Estoque.Authorization;
using MS_Estoque.DTOs.Saldos;
using MS_Estoque.Services.Interfaces;

namespace MS_Estoque.Controllers;

[ApiController]
[Route("api/estoque/saldos")]
[Authorize(Policy = FeaturesEstoque.Estoque)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class SaldosController(
    ISaldoService saldoService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SaldosPaginadosResponseDTO>> Listar(
        [FromQuery] ListarSaldosDTO filtro,
        CancellationToken cancellationToken)
    {
        return Ok(await saldoService.ListarAsync(
            filtro, cancellationToken));
    }

    [HttpPatch("{produtoId:long}/minimo")]
    public async Task<IActionResult> ConfigurarMinimo(
        long produtoId,
        ConfigurarEstoqueMinimoDTO dto,
        CancellationToken cancellationToken)
    {
        try
        {
            await saldoService.ConfigurarMinimoAsync(
                produtoId, dto, cancellationToken);

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }
}
