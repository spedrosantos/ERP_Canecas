using MS_Identidade.DTOs.Empresas;

namespace MS_Identidade.Services.Interfaces;

public interface IEmpresaTemaService
{
    Task<EmpresaTemaResponseDTO?> ObterPorEmpresaIdAsync(long empresaId);
}
