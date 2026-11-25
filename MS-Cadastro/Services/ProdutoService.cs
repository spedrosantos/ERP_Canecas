using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Npgsql;
using MS_Cadastro.Data.Interfaces;
using MS_Cadastro.DTOs.Produtos;
using MS_Cadastro.Entities;
using MS_Cadastro.Repositories;
using MS_Cadastro.Services.Interfaces;
using MS_Cadastro.Data;

namespace MS_Cadastro.Services;

public class ProdutoService : IProdutoService
{
    private readonly ITenantContextFactory _contextFactory;

    public ProdutoService(ITenantContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private static ProdutoResponseDTO Mapear(Produto produto)
    {
        return new ProdutoResponseDTO(
            produto.Id,
            produto.CategoriaId,
            produto.Categoria.Nome,
            produto.UnidadeMedidaId,
            produto.UnidadeMedida.Sigla,
            produto.Sku,
            produto.Nome,
            produto.Descricao,
            produto.Tipo,
            produto.PrecoCusto,
            produto.PrecoVenda,
            produto.EstoqueMinimo,
            produto.Ativo,
            produto.CriadoEm,
            produto.AtualizadoEm
        );
    }
    public async Task<ProdutosPaginadosResponseDTO> ListarAsync(
    ListarProdutosDTO filtro)
    {
        await using var context =
            await _contextFactory.CriarAsync();

        var repository = new ProdutoRepository(context);

        var (itens, total) = await repository.ListarAsync(filtro);

        return new ProdutosPaginadosResponseDTO(
            itens.Select(Mapear).ToList(),
            total,
            filtro.Pagina,
            filtro.TamanhoPagina
        );
    }
    public async Task<ProdutoResponseDTO?> ObterPorIdAsync(
    long id)
    {
        await using var context =
            await _contextFactory.CriarAsync();

        var repository = new ProdutoRepository(context);

        var produto = await repository.ObterPorIdAsync(id);

        return produto is null ? null : Mapear(produto);
    }
    public async Task<ProdutoResponseDTO> CadastrarAsync(
    SalvarProdutoDTO dto)
    {
        await using var context =
            await _contextFactory.CriarAsync();

        var repository = new ProdutoRepository(context);
        var produto = new Produto();

        await PreencherProdutoAsync(context, produto, dto);

        if (await repository.SkuExisteAsync(produto.Sku))
            throw new CadastroDuplicadoException("SKU já cadastrado.");

        repository.Adicionar(produto);

        await SalvarProdutoAsync(repository);

        return Mapear(produto);
    }
    public async Task<ProdutoResponseDTO?> AtualizarAsync(
    long id, SalvarProdutoDTO dto)
    {
        await using var context =
            await _contextFactory.CriarAsync();

        var repository = new ProdutoRepository(context);

        var produto = await repository.ObterPorIdAsync(id);

        if (produto is null)
            return null;

        await PreencherProdutoAsync(context, produto, dto);

        if (await repository.SkuExisteAsync(produto.Sku, id))
            throw new CadastroDuplicadoException("SKU já cadastrado.");

        produto.AtualizadoEm = DateTime.UtcNow;

        await SalvarProdutoAsync(repository);

        return Mapear(produto);
    }
    public async Task<bool> AlterarSituacaoAsync(
    long id, bool ativo)
    {
        await using var context =
            await _contextFactory.CriarAsync();

        var repository = new ProdutoRepository(context);

        var produto = await repository.ObterPorIdAsync(id);

        if (produto is null)
            return false;

        if (ativo && (!produto.Categoria.Ativo ||
                      !produto.UnidadeMedida.Ativo))
        {
            throw new ValidationException(
                "Categoria ou unidade de medida inativa.");
        }

        produto.Ativo = ativo;
        produto.AtualizadoEm = DateTime.UtcNow;

        await repository.SalvarAsync();

        return true;
    }
    private static async Task PreencherProdutoAsync(
    TenantDbContext context,
    Produto produto,
    SalvarProdutoDTO dto)
    {
        var categoria = await context.Categorias
            .FirstOrDefaultAsync(x =>
                x.Id == dto.CategoriaId && x.Ativo);

        if (categoria is null)
            throw new ValidationException("Categoria inválida ou inativa.");

        var unidade = await context.UnidadesMedida
            .FirstOrDefaultAsync(x =>
                x.Id == dto.UnidadeMedidaId && x.Ativo);

        if (unidade is null)
            throw new ValidationException("Unidade inválida ou inativa.");

        if (decimal.Round(dto.PrecoCusto, 4) != dto.PrecoCusto ||
            decimal.Round(dto.PrecoVenda, 2) != dto.PrecoVenda ||
            decimal.Round(dto.EstoqueMinimo, 3) != dto.EstoqueMinimo)
        {
            throw new ValidationException(
                "Quantidade de casas decimais inválida.");
        }

        produto.CategoriaId = categoria.Id;
        produto.Categoria = categoria;

        produto.UnidadeMedidaId = unidade.Id;
        produto.UnidadeMedida = unidade;

        produto.Sku = dto.Sku.Trim().ToUpperInvariant();
        produto.Nome = dto.Nome.Trim();
        produto.Descricao = dto.Descricao?.Trim();
        produto.Tipo = dto.Tipo;
        produto.PrecoCusto = dto.PrecoCusto;
        produto.PrecoVenda = dto.PrecoVenda;
        produto.EstoqueMinimo = dto.EstoqueMinimo;
    }
    private static async Task SalvarProdutoAsync(
    ProdutoRepository repository)
    {
        try
        {
            await repository.SalvarAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg &&
                  pg.SqlState == PostgresErrorCodes.UniqueViolation &&
                  pg.ConstraintName == "produtos_sku_key")
        {
            throw new CadastroDuplicadoException("SKU já cadastrado.");
        }
    }
}