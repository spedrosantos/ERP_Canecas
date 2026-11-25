using MS_Cadastro.Entities;

namespace MS_Cadastro.Repositories.Interfaces;

public interface IUnidadeMedidaRepository
{
    Task<List<UnidadeMedida>> ListarAsync(bool? ativo = null);
    Task<UnidadeMedida?> ObterPorIdAsync(long id);
    Task<bool> SiglaExisteAsync(string sigla, long? ignorarId = null);
    void Adicionar(UnidadeMedida unidade);
    Task SalvarAsync();
}