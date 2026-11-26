using System.Text.Json.Serialization;
using MS_Cadastro.DTOs.Consultas;
using MS_Cadastro.Services.Interfaces;
using MS_Cadastro.Validations;

namespace MS_Cadastro.Services;

public class ConsultaCnpjService(HttpClient http) : IConsultaCnpjService
{
    public async Task<ResultadoConsulta<EmpresaConsultaDTO>> ConsultarAsync(
        string? cnpj, CancellationToken cancellationToken = default)
    {
        if (!DocumentoFiscal.TentarNormalizarCnpj(cnpj, out var normalizado))
            return new(SituacaoConsulta.EntradaInvalida);

        var resultado = await ConsultaHttp.ConsultarAsync<BrasilApiResposta>(
            http, $"api/cnpj/v1/{normalizado}", cancellationToken);
        var dados = resultado.Dados;
        if (dados is null)
            return new(resultado.Situacao, TentarAposSegundos: resultado.TentarAposSegundos);

        if (!DocumentoFiscal.TentarNormalizarCnpj(dados.Cnpj, out var retornado)
            || retornado != normalizado || string.IsNullOrWhiteSpace(dados.RazaoSocial))
            return new(SituacaoConsulta.Indisponivel);

        var cep = ConsultaCepService.TentarNormalizar(dados.Cep, out var cepNormalizado) ? cepNormalizado : null;
        var endereco = new EnderecoConsultaDTO(
            cep, dados.Logradouro, dados.Numero, dados.Complemento, dados.Bairro, dados.Municipio, dados.Uf);

        return new(SituacaoConsulta.Encontrado,
            new(normalizado, dados.RazaoSocial, dados.NomeFantasia, endereco));
    }

    private record BrasilApiResposta(
        string? Cnpj,
        [property: JsonPropertyName("razao_social")] string? RazaoSocial,
        [property: JsonPropertyName("nome_fantasia")] string? NomeFantasia,
        string? Cep, string? Logradouro, string? Numero, string? Complemento,
        string? Bairro, string? Municipio, string? Uf);
}
