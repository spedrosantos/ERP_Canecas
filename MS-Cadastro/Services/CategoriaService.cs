using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Npgsql;
using MS_Cadastro.Data.Interfaces;
using MS_Cadastro.DTOs.Categorias;
using MS_Cadastro.Entities;
using MS_Cadastro.Repositories;
using MS_Cadastro.Services.Interfaces;

namespace MS_Cadastro.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ITenantContextFactory _contextFactory;

    public CategoriaService(ITenantContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<CategoriaResponseDTO>> ListarAsync(
        bool? ativo = null)
    {
        await using var context = await _contextFactory.CriarAsync();

        var repository = new CategoriaRepository(context);

        var categorias = await repository.ListarAsync(ativo);

        return categorias.Select(Mapear).ToList();
    }

    public async Task<CategoriaResponseDTO?> ObterPorIdAsync(long id)
    {
        await using var context = await _contextFactory.CriarAsync();

        var repository = new CategoriaRepository(context);

        var categoria = await repository.ObterPorIdAsync(id);

        return categoria is null ? null : Mapear(categoria);
    }

    public async Task<CategoriaResponseDTO> CadastrarAsync(
        SalvarCategoriaDTO dto)
    {
        await using var context = await _contextFactory.CriarAsync();

        var repository = new CategoriaRepository(context);

        var nome = dto.Nome.Trim().ToUpperInvariant();

        if (await repository.NomeExisteAsync(nome))
            throw new CadastroDuplicadoException(
                "Categoria já cadastrada.");

        var categoria = new Categoria
        {
            Nome = nome
        };

        repository.Adicionar(categoria);

        await SalvarAsync(repository);

        return Mapear(categoria);
    }

    public async Task<CategoriaResponseDTO?> AtualizarAsync(
        long id, SalvarCategoriaDTO dto)
    {
        await using var context = await _contextFactory.CriarAsync();

        var repository = new CategoriaRepository(context);

        var categoria = await repository.ObterPorIdAsync(id);

        if (categoria is null)
            return null;

        var nome = dto.Nome.Trim().ToUpperInvariant();

        if (await repository.NomeExisteAsync(nome, id))
            throw new CadastroDuplicadoException(
                "Categoria já cadastrada.");

        categoria.Nome = nome;

        await SalvarAsync(repository);

        return Mapear(categoria);
    }

    public async Task<bool> AlterarSituacaoAsync(long id, bool ativo)
    {
        await using var context = await _contextFactory.CriarAsync();

        var repository = new CategoriaRepository(context);

        var categoria = await repository.ObterPorIdAsync(id);

        if (categoria is null)
            return false;

        if (!ativo && await context.Produtos.AnyAsync(x =>
            x.CategoriaId == id && x.Ativo))
        {
            throw new ValidationException(
                "Existem produtos ativos vinculados a esta categoria.");
        }

        categoria.Ativo = ativo;

        await repository.SalvarAsync();

        return true;
    }

    private static async Task SalvarAsync(
        CategoriaRepository repository)
    {
        try
        {
            await repository.SalvarAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg &&
                  pg.SqlState == PostgresErrorCodes.UniqueViolation &&
                  pg.ConstraintName == "categorias_nome_key")
        {
            throw new CadastroDuplicadoException(
                "Categoria já cadastrada.");
        }
    }

    private static CategoriaResponseDTO Mapear(Categoria categoria)
    {
        return new CategoriaResponseDTO(
            categoria.Id,
            categoria.Nome,
            categoria.Ativo
        );
    }
}