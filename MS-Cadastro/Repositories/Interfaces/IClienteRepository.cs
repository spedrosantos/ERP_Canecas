using MS_Cadastro.DTOs.Clientes;
using MS_Cadastro.Entities;

namespace MS_Cadastro.Repositories.Interfaces;

public interface IClienteRepository
{
    Task<(List<Cliente> Itens, long Total)> ListarAsync(ListarClientesDTO filtro, CancellationToken cancellationToken);
    Task<Cliente?> ObterPorIdAsync(long id, CancellationToken cancellationToken);
    Task<Cliente?> ObterParaAtualizacaoAsync(long id, CancellationToken cancellationToken);
    void Adicionar(Cliente cliente);
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
