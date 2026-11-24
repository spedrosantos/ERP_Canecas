using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Cadastro.Authorization;
using MS_Cadastro.DTOs.Clientes;
using MS_Cadastro.Services;
using MS_Cadastro.Services.Interfaces;

namespace MS_Cadastro.Controllers;

[ApiController]
[Route("api/clientes")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ClientesController(IClienteService clienteService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = FeaturesCadastro.Cliente)]
    public async Task<ActionResult<ClientesPaginadosResponseDTO>> Listar(
        [FromQuery] ListarClientesDTO filtro, CancellationToken cancellationToken)
    {
        return Ok(await clienteService.ListarAsync(filtro, cancellationToken));
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = FeaturesCadastro.Cliente)]
    public async Task<ActionResult<ClienteResponseDTO>> Obter(
        [Range(1, long.MaxValue)] long id, CancellationToken cancellationToken)
    {
        var cliente = await clienteService.ObterPorIdAsync(id, cancellationToken);

        return cliente is null ? NotFound() : Ok(cliente);
    }

    [HttpPost]
    [Authorize(Policy = FeaturesCadastro.ClienteCadastrar)]
    public async Task<ActionResult<ClienteResponseDTO>> Cadastrar(
        SalvarClienteDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var cliente = await clienteService.CadastrarAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(Obter), new { id = cliente.Id }, cliente);
        }
        catch (ClienteDocumentoDuplicadoException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
        }
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = FeaturesCadastro.ClienteEditar)]
    public async Task<ActionResult<ClienteResponseDTO>> Editar(
        [Range(1, long.MaxValue)] long id, SalvarClienteDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var cliente = await clienteService.EditarAsync(id, dto, cancellationToken);
            return cliente is null ? NotFound() : Ok(cliente);
        }
        catch (ClienteDocumentoDuplicadoException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
        }
    }

    [HttpPatch("{id:long}/situacao")]
    [Authorize(Policy = FeaturesCadastro.ClienteEditar)]
    public async Task<ActionResult<ClienteResponseDTO>> AlterarSituacao(
        [Range(1, long.MaxValue)] long id, AlterarSituacaoClienteDTO dto, CancellationToken cancellationToken)
    {
        var cliente = await clienteService.AlterarSituacaoAsync(id, dto.Ativo.GetValueOrDefault(), cancellationToken);

        return cliente is null ? NotFound() : Ok(cliente);
    }
}
