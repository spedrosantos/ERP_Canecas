using MS_Cadastro.DTOs.Categorias;

namespace MS_Cadastro.Services.Interfaces;

public interface ICategoriaService
{
    Task<List<CategoriaResponseDTO>> ListarAsync(bool? ativo = null);

    Task<CategoriaResponseDTO?> ObterPorIdAsync(long id);

    Task<CategoriaResponseDTO> CadastrarAsync(SalvarCategoriaDTO dto);

    Task<CategoriaResponseDTO?> AtualizarAsync(
        long id, SalvarCategoriaDTO dto);

    Task<bool> AlterarSituacaoAsync(long id, bool ativo);
}