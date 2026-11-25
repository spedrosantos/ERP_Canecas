using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Cadastro.Authorization;
using MS_Cadastro.DTOs.Fornecedores;
using MS_Cadastro.Services;
using MS_Cadastro.Services.Interfaces;

namespace MS_Cadastro.Controllers;

[ApiController]
[Route("api/fornecedores")]
[Authorize(Policy = FeaturesCadastro.Fornecedor)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class FornecedoresController(IFornecedorService fornecedorService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<FornecedoresPaginadosResponseDTO>> Listar(
        [FromQuery] ListarFornecedoresDTO filtro, CancellationToken cancellationToken)
    {
        return Ok(await fornecedorService.ListarAsync(filtro, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<FornecedorResponseDTO>> Obter(
        [Range(1, long.MaxValue)] long id, CancellationToken cancellationToken)
    {
        var fornecedor = await fornecedorService.ObterPorIdAsync(id, cancellationToken);

        return fornecedor is null ? NotFound() : Ok(fornecedor);
    }

    [HttpPost]
    public async Task<ActionResult<FornecedorResponseDTO>> Cadastrar(
        SalvarFornecedorDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var fornecedor = await fornecedorService.CadastrarAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(Obter), new { id = fornecedor.Id }, fornecedor);
        }
        catch (FornecedorDocumentoDuplicadoException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
        }
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<FornecedorResponseDTO>> Editar(
        [Range(1, long.MaxValue)] long id, SalvarFornecedorDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var fornecedor = await fornecedorService.EditarAsync(id, dto, cancellationToken);
            return fornecedor is null ? NotFound() : Ok(fornecedor);
        }
        catch (FornecedorDocumentoDuplicadoException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message);
        }
    }

    [HttpPatch("{id:long}/situacao")]
    public async Task<ActionResult<FornecedorResponseDTO>> AlterarSituacao(
        [Range(1, long.MaxValue)] long id, AlterarSituacaoFornecedorDTO dto, CancellationToken cancellationToken)
    {
        var fornecedor = await fornecedorService.AlterarSituacaoAsync(id, dto.Ativo.GetValueOrDefault(), cancellationToken);

        return fornecedor is null ? NotFound() : Ok(fornecedor);
    }
}
