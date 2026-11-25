using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Npgsql;
using MS_Cadastro.Data.Interfaces;
using MS_Cadastro.DTOs.UnidadesMedida;
using MS_Cadastro.Entities;
using MS_Cadastro.Repositories;
using MS_Cadastro.Services.Interfaces;

namespace MS_Cadastro.Services;

public class UnidadeMedidaService : IUnidadeMedidaService
{
    private readonly ITenantContextFactory _contextFactory;

    public UnidadeMedidaService(ITenantContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<UnidadeMedidaResponseDTO>> ListarAsync(
        bool? ativo = null)
    {
        await using var context = await _contextFactory.CriarAsync();

        var repository = new UnidadeMedidaRepository(context);

        var unidades = await repository.ListarAsync(ativo);

        return unidades.Select(Mapear).ToList();
    }

    public async Task<UnidadeMedidaResponseDTO?> ObterPorIdAsync(long id)
    {
        await using var context = await _contextFactory.CriarAsync();

        var repository = new UnidadeMedidaRepository(context);

        var unidade = await repository.ObterPorIdAsync(id);

        return unidade is null ? null : Mapear(unidade);
    }

    public async Task<UnidadeMedidaResponseDTO> CadastrarAsync(
        SalvarUnidadeMedidaDTO dto)
    {
        await using var context = await _contextFactory.CriarAsync();

        var repository = new UnidadeMedidaRepository(context);

        var sigla = dto.Sigla.Trim().ToUpperInvariant();

        if (await repository.SiglaExisteAsync(sigla))
            throw new CadastroDuplicadoException(
                "Unidade de medida já cadastrada.");

        var unidade = new UnidadeMedida
        {
            Sigla = sigla,
            Descricao = dto.Descricao.Trim()
        };

        repository.Adicionar(unidade);

        await SalvarAsync(repository);

        return Mapear(unidade);
    }

    public async Task<UnidadeMedidaResponseDTO?> AtualizarAsync(
        long id, SalvarUnidadeMedidaDTO dto)
    {
        await using var context = await _contextFactory.CriarAsync();

        var repository = new UnidadeMedidaRepository(context);

        var unidade = await repository.ObterPorIdAsync(id);

        if (unidade is null)
            return null;

        var sigla = dto.Sigla.Trim().ToUpperInvariant();

        if (await repository.SiglaExisteAsync(sigla, id))
            throw new CadastroDuplicadoException(
                "Unidade de medida já cadastrada.");

        unidade.Sigla = sigla;
        unidade.Descricao = dto.Descricao.Trim();

        await SalvarAsync(repository);

        return Mapear(unidade);
    }

    public async Task<bool> AlterarSituacaoAsync(long id, bool ativo)
    {
        await using var context = await _contextFactory.CriarAsync();

        var repository = new UnidadeMedidaRepository(context);

        var unidade = await repository.ObterPorIdAsync(id);

        if (unidade is null)
            return false;

        if (!ativo && await context.Produtos.AnyAsync(x =>
            x.UnidadeMedidaId == id && x.Ativo))
        {
            throw new ValidationException(
                "Existem produtos ativos vinculados a esta unidade.");
        }

        unidade.Ativo = ativo;

        await repository.SalvarAsync();

        return true;
    }

    private static async Task SalvarAsync(
        UnidadeMedidaRepository repository)
    {
        try
        {
            await repository.SalvarAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg &&
                  pg.SqlState == PostgresErrorCodes.UniqueViolation &&
                  pg.ConstraintName == "unidades_medida_sigla_key")
        {
            throw new CadastroDuplicadoException(
                "Unidade de medida já cadastrada.");
        }
    }

    private static UnidadeMedidaResponseDTO Mapear(
        UnidadeMedida unidade)
    {
        return new UnidadeMedidaResponseDTO(
            unidade.Id,
            unidade.Sigla,
            unidade.Descricao,
            unidade.Ativo
        );
    }
}