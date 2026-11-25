using MS_Cadastro.Entities;

namespace MS_Cadastro.Repositories.Interfaces;

public interface ICategoriaRepository
{
    Task<List<Categoria>> ListarAsync(bool? ativo = null);
    Task<Categoria?> ObterPorIdAsync(long id);
    Task<bool> NomeExisteAsync(string nome, long? ignorarId = null);
    void Adicionar(Categoria categoria);
    Task SalvarAsync();
}