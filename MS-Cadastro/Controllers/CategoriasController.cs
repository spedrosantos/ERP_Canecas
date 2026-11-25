using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Cadastro.Authorization;
using MS_Cadastro.DTOs.Categorias;
using MS_Cadastro.DTOs.Produtos;
using MS_Cadastro.Services;
using MS_Cadastro.Services.Interfaces;

namespace MS_Cadastro.Controllers;

[ApiController]
[Route("api/categorias")]
[Authorize(Policy = FeaturesCadastro.Produto)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CategoriasController(
    ICategoriaService categoriaService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoriaResponseDTO>>> Listar(
        [FromQuery] bool? ativo)
    {
        return Ok(await categoriaService.ListarAsync(ativo));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CategoriaResponseDTO>> Obter(long id)
    {
        var categoria = await categoriaService.ObterPorIdAsync(id);

        return categoria is null ? NotFound() : Ok(categoria);
    }

    [HttpPost]
    public async Task<ActionResult<CategoriaResponseDTO>> Cadastrar(
        SalvarCategoriaDTO dto)
    {
        try
        {
            var categoria = await categoriaService.CadastrarAsync(dto);

            return CreatedAtAction(
                nameof(Obter),
                new { id = categoria.Id },
                categoria);
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
    public async Task<ActionResult<CategoriaResponseDTO>> Atualizar(
        long id, SalvarCategoriaDTO dto)
    {
        try
        {
            var categoria = await categoriaService.AtualizarAsync(id, dto);

            return categoria is null ? NotFound() : Ok(categoria);
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
            var alterado = await categoriaService.AlterarSituacaoAsync(
                id, dto.Ativo!.Value);

            return alterado ? NoContent() : NotFound();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }
}