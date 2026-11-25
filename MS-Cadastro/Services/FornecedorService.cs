using Microsoft.EntityFrameworkCore;
using MS_Cadastro.Data.Interfaces;
using MS_Cadastro.DTOs.Fornecedores;
using MS_Cadastro.Entities;
using MS_Cadastro.Repositories;
using MS_Cadastro.Repositories.Interfaces;
using MS_Cadastro.Services.Interfaces;
using Npgsql;

namespace MS_Cadastro.Services;

public class FornecedorService(ITenantContextFactory contextFactory) : IFornecedorService
{
    public async Task<FornecedoresPaginadosResponseDTO> ListarAsync(
        ListarFornecedoresDTO filtro, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CriarAsync(cancellationToken);
        var repository = new FornecedorRepository(context);
        var (itens, total) = await repository.ListarAsync(filtro, cancellationToken);

        return new(itens.ConvertAll(MapearFornecedor), total, filtro.Pagina, filtro.TamanhoPagina);
    }

    public async Task<FornecedorResponseDTO?> ObterPorIdAsync(long id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CriarAsync(cancellationToken);
        var repository = new FornecedorRepository(context);
        var fornecedor = await repository.ObterPorIdAsync(id, cancellationToken);

        return fornecedor is null ? null : MapearFornecedor(fornecedor);
    }

    public async Task<FornecedorResponseDTO> CadastrarAsync(
        SalvarFornecedorDTO dto, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CriarAsync(cancellationToken);
        var repository = new FornecedorRepository(context);
        var fornecedor = new Fornecedor();
        AplicarCadastro(fornecedor, dto);
        repository.Adicionar(fornecedor);

        await SalvarCadastroAsync(repository, cancellationToken);

        return MapearFornecedor(fornecedor);
    }

    public async Task<FornecedorResponseDTO?> EditarAsync(
        long id, SalvarFornecedorDTO dto, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CriarAsync(cancellationToken);
        var repository = new FornecedorRepository(context);
        var fornecedor = await repository.ObterParaAtualizacaoAsync(id, cancellationToken);
        if (fornecedor is null)
            return null;

        AplicarCadastro(fornecedor, dto);
        fornecedor.AtualizadoEm = DateTime.UtcNow;
        await SalvarCadastroAsync(repository, cancellationToken);

        return MapearFornecedor(fornecedor);
    }

    public async Task<FornecedorResponseDTO?> AlterarSituacaoAsync(
        long id, bool ativo, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CriarAsync(cancellationToken);
        var repository = new FornecedorRepository(context);
        var fornecedor = await repository.ObterParaAtualizacaoAsync(id, cancellationToken);
        if (fornecedor is null)
            return null;

        if (fornecedor.Ativo != ativo)
        {
            fornecedor.Ativo = ativo;
            fornecedor.AtualizadoEm = DateTime.UtcNow;
            await repository.SalvarAlteracoesAsync(cancellationToken);
        }

        return MapearFornecedor(fornecedor);
    }

    private static async Task SalvarCadastroAsync(
        IFornecedorRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            await repository.SalvarAlteracoesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "uq_fornecedor_cpf_cnpj" })
        {
            throw new FornecedorDocumentoDuplicadoException(ex);
        }
    }

    private static void AplicarCadastro(Fornecedor fornecedor, SalvarFornecedorDTO dto)
    {
        fornecedor.TipoPessoa = dto.TipoPessoa.ToUpperInvariant();
        fornecedor.Nome = dto.Nome.Trim();
        fornecedor.NomeFantasia = Limpar(dto.NomeFantasia);
        fornecedor.CpfCnpj = dto.CpfCnpj;
        fornecedor.Telefone = Limpar(dto.Telefone);
        fornecedor.Email = Limpar(dto.Email);
        fornecedor.Cep = Limpar(dto.Cep);
        fornecedor.Logradouro = Limpar(dto.Logradouro);
        fornecedor.Numero = Limpar(dto.Numero);
        fornecedor.Complemento = Limpar(dto.Complemento);
        fornecedor.Bairro = Limpar(dto.Bairro);
        fornecedor.Cidade = Limpar(dto.Cidade);
        fornecedor.Uf = Limpar(dto.Uf)?.ToUpperInvariant();
    }

    private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static FornecedorResponseDTO MapearFornecedor(Fornecedor fornecedor)
    {
        return new(
            fornecedor.Id, fornecedor.TipoPessoa, fornecedor.Nome, fornecedor.NomeFantasia,
            fornecedor.CpfCnpj, fornecedor.Telefone, fornecedor.Email, fornecedor.Cep,
            fornecedor.Logradouro, fornecedor.Numero, fornecedor.Complemento, fornecedor.Bairro,
            fornecedor.Cidade, fornecedor.Uf, fornecedor.Ativo, fornecedor.CriadoEm, fornecedor.AtualizadoEm);
    }
}
