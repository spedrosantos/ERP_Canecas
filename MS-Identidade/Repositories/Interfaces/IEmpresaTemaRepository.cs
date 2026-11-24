using MS_Identidade.Entities;

namespace MS_Identidade.Repositories.Interfaces;

public interface IEmpresaTemaRepository
{
    Task<EmpresaTema?> ObterPorEmpresaIdAsync(long empresaId);
}
