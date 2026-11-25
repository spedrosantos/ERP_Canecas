using MS_Cadastro.DTOs.Fornecedores;

namespace MS_Cadastro.Services.Interfaces;

public interface IFornecedorService
{
    Task<FornecedoresPaginadosResponseDTO> ListarAsync(ListarFornecedoresDTO filtro, CancellationToken cancellationToken);
    Task<FornecedorResponseDTO?> ObterPorIdAsync(long id, CancellationToken cancellationToken);
    Task<FornecedorResponseDTO> CadastrarAsync(SalvarFornecedorDTO dto, CancellationToken cancellationToken);
    Task<FornecedorResponseDTO?> EditarAsync(long id, SalvarFornecedorDTO dto, CancellationToken cancellationToken);
    Task<FornecedorResponseDTO?> AlterarSituacaoAsync(long id, bool ativo, CancellationToken cancellationToken);
}
