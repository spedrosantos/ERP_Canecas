using Financeiro;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Venda.Authorization;
using MS_Venda.DTOs.Pedidos;
using MS_Venda.Services;
using MS_Venda.Services.Interfaces;

namespace MS_Venda.Controllers;

[ApiController]
[Route("api/vendas/pedidos")]
[Authorize(Policy = FeaturesVenda.Venda)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PedidosController(IPedidoService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginadoResponseDTO<PedidoResponseDTO>>> Listar(
        [FromQuery] ListarPedidosDTO filtro, CancellationToken ct)
    {
        try
        {
            return Ok(await service.ListarAsync(filtro, ct));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpGet("{id:long}")]
    public Task<ActionResult<PedidoDetalheResponseDTO>> Obter(long id, CancellationToken ct) =>
        ExecutarAsync(() => service.ObterAsync(id, ct));

    [HttpGet("clientes")]
    public Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarClientes(
        [FromQuery] ListarCadastrosDTO filtro, CancellationToken ct) =>
        service.ListarClientesAsync(filtro, ct);

    [HttpGet("produtos")]
    public Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarProdutos(
        [FromQuery] ListarCadastrosDTO filtro, CancellationToken ct) =>
        service.ListarProdutosAsync(filtro, ct);

    [HttpGet("produtos/{produtoId:long}/disponibilidade")]
    public async Task<ActionResult<DisponibilidadeProdutoResponseDTO>> ObterDisponibilidade(
        long produtoId, CancellationToken ct)
    {
        var disponibilidade = await service.ObterDisponibilidadeAsync(produtoId, ct);
        return disponibilidade is null ? NotFound() : Ok(disponibilidade);
    }

    [HttpPost]
    public Task<ActionResult<PedidoDetalheResponseDTO>> Cadastrar(SalvarPedidoDTO dto, CancellationToken ct) =>
        ExecutarAsync(async () => await service.CadastrarAsync(dto, ct), criado: true);

    [HttpPut("{id:long}")]
    public Task<ActionResult<PedidoDetalheResponseDTO>> Editar(long id, SalvarPedidoDTO dto, CancellationToken ct) =>
        ExecutarAsync(() => service.EditarAsync(id, dto, ct));

    [HttpPost("{id:long}/cancelar")]
    public Task<ActionResult<PedidoDetalheResponseDTO>> Cancelar(long id, CancellationToken ct) =>
        ExecutarAsync(() => service.CancelarAsync(id, ct));

    [HttpPost("{id:long}/confirmar")]
    public Task<ActionResult<PedidoDetalheResponseDTO>> Confirmar(
        long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ConfirmarPedidoDTO? dto, CancellationToken ct) =>
        ExecutarAsync(() => service.ConfirmarAsync(id, dto, ct));

    private async Task<ActionResult<PedidoDetalheResponseDTO>> ExecutarAsync(
        Func<Task<PedidoDetalheResponseDTO?>> executar, bool criado = false)
    {
        try
        {
            var pedido = await executar();
            if (pedido is null)
                return NotFound();
            if (criado)
                return CreatedAtAction(nameof(Obter), new { id = pedido.Pedido.Id }, pedido);

            return Ok(pedido);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (PedidoSituacaoException ex)
        {
            return Conflict(new { mensagem = ex.Message });
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
