using MS_Cadastro.DTOs.UnidadesMedida;

namespace MS_Cadastro.Services.Interfaces;

public interface IUnidadeMedidaService
{
    Task<List<UnidadeMedidaResponseDTO>> ListarAsync(bool? ativo = null);

    Task<UnidadeMedidaResponseDTO?> ObterPorIdAsync(long id);

    Task<UnidadeMedidaResponseDTO> CadastrarAsync(
        SalvarUnidadeMedidaDTO dto);

    Task<UnidadeMedidaResponseDTO?> AtualizarAsync(
        long id, SalvarUnidadeMedidaDTO dto);

    Task<bool> AlterarSituacaoAsync(long id, bool ativo);
}