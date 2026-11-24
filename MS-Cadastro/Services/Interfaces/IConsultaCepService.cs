using MS_Cadastro.DTOs.Consultas;

namespace MS_Cadastro.Services.Interfaces;

public interface IConsultaCepService
{
    Task<ResultadoConsulta<EnderecoConsultaDTO>> ConsultarAsync(
        string? cep, CancellationToken cancellationToken = default);
}
