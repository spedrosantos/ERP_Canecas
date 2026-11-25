using MS_Cadastro.DTOs.Consultas;

namespace MS_Cadastro.Services.Interfaces;

public interface IConsultaCnpjService
{
    Task<ResultadoConsulta<EmpresaConsultaDTO>> ConsultarAsync(
        string? cnpj, CancellationToken cancellationToken = default);
}
