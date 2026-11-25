using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Cadastro.Authorization;
using MS_Cadastro.DTOs.Consultas;
using MS_Cadastro.Services;
using MS_Cadastro.Services.Interfaces;

namespace MS_Cadastro.Controllers;

[ApiController]
[Route("api/consultas")]
[Authorize(Policy = FeaturesCadastro.SessaoAtiva)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ConsultasController(IConsultaCepService cepService, IConsultaCnpjService cnpjService)
    : ControllerBase
{
    [HttpGet("cep")]
    public async Task<ActionResult<EnderecoConsultaDTO>> ConsultarCep(
        [FromQuery] string? cep, CancellationToken cancellationToken)
    {
        var resultado = await cepService.ConsultarAsync(cep, cancellationToken);
        return Responder(resultado, "CEP");
    }

    [HttpGet("cnpj")]
    public async Task<ActionResult<EmpresaConsultaDTO>> ConsultarCnpj(
        [FromQuery] string? cnpj, CancellationToken cancellationToken)
    {
        var resultado = await cnpjService.ConsultarAsync(cnpj, cancellationToken);
        return Responder(resultado, "CNPJ");
    }

    private ActionResult<T> Responder<T>(ResultadoConsulta<T> resultado, string tipo) where T : class
    {
        return resultado.Situacao switch
        {
            SituacaoConsulta.Encontrado => Ok(resultado.Dados),
            SituacaoConsulta.EntradaInvalida => Problem(
                statusCode: StatusCodes.Status400BadRequest, title: $"{tipo} inválido."),
            SituacaoConsulta.NaoEncontrado => Problem(
                statusCode: StatusCodes.Status404NotFound, title: $"{tipo} não encontrado."),
            SituacaoConsulta.TempoLimite => Problem(
                statusCode: StatusCodes.Status504GatewayTimeout, title: "A consulta excedeu o tempo limite."),
            _ => Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Serviço de consulta indisponível.")
        };
    }
}
