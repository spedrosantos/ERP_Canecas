using Microsoft.EntityFrameworkCore;
using MS_Cadastro.Data.Interfaces;
using MS_Cadastro.DTOs.Clientes;
using MS_Cadastro.Entities;
using MS_Cadastro.Repositories;
using MS_Cadastro.Repositories.Interfaces;
using MS_Cadastro.Services.Interfaces;
using Npgsql;

namespace MS_Cadastro.Services;

public class ClienteService(ITenantContextFactory contextFactory) : IClienteService
{
    public async Task<ClientesPaginadosResponseDTO> ListarAsync(
        ListarClientesDTO filtro, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CriarAsync(cancellationToken);
        var repository = new ClienteRepository(context);
        var (itens, total) = await repository.ListarAsync(filtro, cancellationToken);

        return new(itens.ConvertAll(MapearCliente), total, filtro.Pagina, filtro.TamanhoPagina);
    }

    public async Task<ClienteResponseDTO?> ObterPorIdAsync(long id, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CriarAsync(cancellationToken);
        var repository = new ClienteRepository(context);
        var cliente = await repository.ObterPorIdAsync(id, cancellationToken);

        return cliente is null ? null : MapearCliente(cliente);
    }

    public async Task<ClienteResponseDTO> CadastrarAsync(
        SalvarClienteDTO dto, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CriarAsync(cancellationToken);
        var repository = new ClienteRepository(context);
        var cliente = new Cliente();
        AplicarCadastro(cliente, dto);
        repository.Adicionar(cliente);

        await SalvarCadastroAsync(repository, cancellationToken);

        return MapearCliente(cliente);
    }

    public async Task<ClienteResponseDTO?> EditarAsync(
        long id, SalvarClienteDTO dto, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CriarAsync(cancellationToken);
        var repository = new ClienteRepository(context);
        var cliente = await repository.ObterParaAtualizacaoAsync(id, cancellationToken);
        if (cliente is null)
            return null;

        AplicarCadastro(cliente, dto);
        cliente.AtualizadoEm = DateTime.UtcNow;
        await SalvarCadastroAsync(repository, cancellationToken);

        return MapearCliente(cliente);
    }

    public async Task<ClienteResponseDTO?> AlterarSituacaoAsync(
        long id, bool ativo, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CriarAsync(cancellationToken);
        var repository = new ClienteRepository(context);
        var cliente = await repository.ObterParaAtualizacaoAsync(id, cancellationToken);
        if (cliente is null)
            return null;

        if (cliente.Ativo != ativo)
        {
            cliente.Ativo = ativo;
            cliente.AtualizadoEm = DateTime.UtcNow;
            await repository.SalvarAlteracoesAsync(cancellationToken);
        }

        return MapearCliente(cliente);
    }

    private static async Task SalvarCadastroAsync(
        IClienteRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            await repository.SalvarAlteracoesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "uq_cliente_cpf_cnpj" })
        {
            throw new ClienteDocumentoDuplicadoException(ex);
        }
    }

    private static void AplicarCadastro(Cliente cliente, SalvarClienteDTO dto)
    {
        cliente.TipoPessoa = dto.TipoPessoa.ToUpperInvariant();
        cliente.Nome = dto.Nome.Trim();
        cliente.NomeFantasia = Limpar(dto.NomeFantasia);
        cliente.CpfCnpj = dto.CpfCnpj;
        cliente.Telefone = Limpar(dto.Telefone);
        cliente.Email = Limpar(dto.Email);
        cliente.Cep = Limpar(dto.Cep);
        cliente.Logradouro = Limpar(dto.Logradouro);
        cliente.Numero = Limpar(dto.Numero);
        cliente.Complemento = Limpar(dto.Complemento);
        cliente.Bairro = Limpar(dto.Bairro);
        cliente.Cidade = Limpar(dto.Cidade);
        cliente.Uf = Limpar(dto.Uf)?.ToUpperInvariant();
    }

    private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static ClienteResponseDTO MapearCliente(Cliente cliente)
    {
        return new(
            cliente.Id, cliente.TipoPessoa, cliente.Nome, cliente.NomeFantasia,
            cliente.CpfCnpj, cliente.Telefone, cliente.Email, cliente.Cep,
            cliente.Logradouro, cliente.Numero, cliente.Complemento, cliente.Bairro,
            cliente.Cidade, cliente.Uf, cliente.Ativo, cliente.CriadoEm, cliente.AtualizadoEm);
    }
}
