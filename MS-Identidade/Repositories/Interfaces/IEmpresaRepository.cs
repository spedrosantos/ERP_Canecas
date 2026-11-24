using MS_Identidade.Entities;

namespace MS_Identidade.Repositories.Interfaces;

public interface IEmpresaRepository
{
    Task<Empresa?> ObterPorIdAsync(long id);
}