using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using MS_Cadastro.Authorization;
using MS_Cadastro.DTOs.Produtos;
using MS_Cadastro.Services.Interfaces;

namespace MS_Cadastro.Controllers;

[ApiController]
[Route("api/produtos")]
[Authorize(Policy = FeaturesCadastro.Produto)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _service;

    public ProdutosController(IProdutoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] ListarProdutosDTO filtro)
    {
        return Ok(await _service.ListarAsync(filtro));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> ObterPorId(long id)
    {
        var produto = await _service.ObterPorIdAsync(id);

        return produto is null ? NotFound() : Ok(produto);
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar(
        SalvarProdutoDTO dto)
    {
        try
        {
            var produto = await _service.CadastrarAsync(dto);

            return CreatedAtAction(
                nameof(ObterPorId),
                new { id = produto.Id },
                produto);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(
        long id, SalvarProdutoDTO dto)
    {
        try
        {
            var produto = await _service.AtualizarAsync(id, dto);

            return produto is null ? NotFound() : Ok(produto);
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
            var alterado = await _service.AlterarSituacaoAsync(
                id, dto.Ativo!.Value);

            return alterado ? NoContent() : NotFound();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }
}