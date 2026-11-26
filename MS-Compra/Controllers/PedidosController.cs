using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Compra.Authorization;
using MS_Compra.DTOs.Pedidos;
using MS_Compra.Services;
using MS_Compra.Services.Interfaces;

namespace MS_Compra.Controllers;

[ApiController]
[Route("api/compras/pedidos")]
[Authorize(Policy = FeaturesCompra.Compra)]
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

    [HttpGet("fornecedores")]
    public Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarFornecedores(
        [FromQuery] ListarCadastrosDTO filtro, CancellationToken ct) =>
        service.ListarFornecedoresAsync(filtro, ct);

    [HttpGet("produtos")]
    public Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarProdutos(
        [FromQuery] ListarCadastrosDTO filtro, CancellationToken ct) =>
        service.ListarProdutosAsync(filtro, ct);

    [HttpPost]
    public Task<ActionResult<PedidoDetalheResponseDTO>> Cadastrar(SalvarPedidoDTO dto, CancellationToken ct) =>
        ExecutarAsync(async () => await service.CadastrarAsync(dto, ct), criado: true);

    [HttpPut("{id:long}")]
    public Task<ActionResult<PedidoDetalheResponseDTO>> Editar(long id, SalvarPedidoDTO dto, CancellationToken ct) =>
        ExecutarAsync(() => service.EditarAsync(id, dto, ct));

    [HttpPost("{id:long}/cancelar")]
    public Task<ActionResult<PedidoDetalheResponseDTO>> Cancelar(long id, CancellationToken ct) =>
        ExecutarAsync(() => service.CancelarAsync(id, ct));

    [HttpPost("{id:long}/receber")]
    public Task<ActionResult<PedidoDetalheResponseDTO>> Receber(
        long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReceberPedidoDTO? dto, CancellationToken ct) =>
        ExecutarAsync(() => service.ReceberAsync(id, dto, ct));

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
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
