using MS_Cadastro.DTOs.Clientes;

namespace MS_Cadastro.Services.Interfaces;

public interface IClienteService
{
    Task<ClientesPaginadosResponseDTO> ListarAsync(ListarClientesDTO filtro, CancellationToken cancellationToken);
    Task<ClienteResponseDTO?> ObterPorIdAsync(long id, CancellationToken cancellationToken);
    Task<ClienteResponseDTO> CadastrarAsync(SalvarClienteDTO dto, CancellationToken cancellationToken);
    Task<ClienteResponseDTO?> EditarAsync(long id, SalvarClienteDTO dto, CancellationToken cancellationToken);
    Task<ClienteResponseDTO?> AlterarSituacaoAsync(long id, bool ativo, CancellationToken cancellationToken);
}
