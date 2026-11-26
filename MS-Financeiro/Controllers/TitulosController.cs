using System.ComponentModel.DataAnnotations;
using Financeiro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Financeiro.Authorization;
using MS_Financeiro.DTOs;
using MS_Financeiro.Services.Interfaces;

namespace MS_Financeiro.Controllers;

[ApiController]
[Route("api/financeiro/titulos")]
[Authorize(Policy = FeaturesFinanceiro.Financeiro)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TitulosController(ITituloService service) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<TitulosPaginadosResponseDTO>> Listar([FromQuery] ListarTitulosDTO filtro, CancellationToken ct) =>
        ExecutarAsync<TitulosPaginadosResponseDTO>(async () => await service.ListarAsync(filtro, ct));

    [HttpGet("{id:long}")]
    public Task<ActionResult<TituloDetalheResponseDTO>> Obter(long id, CancellationToken ct) =>
        ExecutarAsync(() => service.ObterAsync(id, ct));

    [HttpGet("~/api/financeiro/indicadores")]
    public Task<ActionResult<IndicadoresFinanceirosResponseDTO>> ObterIndicadores(
        [FromQuery] FiltroTitulosDTO filtro, CancellationToken ct) =>
        ExecutarAsync<IndicadoresFinanceirosResponseDTO>(async () => await service.ObterIndicadoresAsync(filtro, ct));

    [HttpPost("{id:long}/baixas")]
    public Task<ActionResult<BaixaResponseDTO>> RegistrarBaixa(long id, RegistrarBaixaDTO dto, CancellationToken ct) =>
        ExecutarAsync(() => service.RegistrarBaixaAsync(id, dto, ct),
            baixa => CreatedAtAction(nameof(Obter), new { id = baixa.TituloId }, baixa));

    [HttpPost("{id:long}/baixas/{baixaId:long}/estornar")]
    public Task<ActionResult<BaixaResponseDTO>> EstornarBaixa(
        long id, long baixaId, EstornarBaixaDTO dto, CancellationToken ct) =>
        ExecutarAsync(() => service.EstornarBaixaAsync(id, baixaId, dto, ct));

    private async Task<ActionResult<T>> ExecutarAsync<T>(Func<Task<T?>> executar, Func<T, ActionResult>? sucesso = null)
        where T : class
    {
        try
        {
            var resultado = await executar();
            if (resultado is null)
                return NotFound();
            return sucesso?.Invoke(resultado) ?? Ok(resultado);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (ConflitoFinanceiroException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
