using System.Text.Json;
using MS_Cadastro.DTOs.Consultas;
using MS_Cadastro.Services.Interfaces;

namespace MS_Cadastro.Services;

public class ConsultaCepService(HttpClient http) : IConsultaCepService
{
    public async Task<ResultadoConsulta<EnderecoConsultaDTO>> ConsultarAsync(
        string? cep, CancellationToken cancellationToken = default)
    {
        if (!TentarNormalizar(cep, out var normalizado))
            return new(SituacaoConsulta.EntradaInvalida);

        var resultado = await ConsultaHttp.ConsultarAsync<ViaCepResposta>(
            http, $"ws/{normalizado}/json/", cancellationToken);
        var dados = resultado.Dados;
        if (dados is null)
            return new(resultado.Situacao, TentarAposSegundos: resultado.TentarAposSegundos);

        if (dados.Erro.ValueKind == JsonValueKind.True
            || (dados.Erro.ValueKind == JsonValueKind.String && dados.Erro.GetString() == "true"))
            return new(SituacaoConsulta.NaoEncontrado);

        if (!TentarNormalizar(dados.Cep, out var retornado) || retornado != normalizado)
            return new(SituacaoConsulta.Indisponivel);

        return new(SituacaoConsulta.Encontrado,
            new(normalizado, dados.Logradouro, null, dados.Complemento, dados.Bairro, dados.Localidade, dados.Uf));
    }

    internal static bool TentarNormalizar(string? cep, out string normalizado)
    {
        normalizado = cep?.Trim() ?? string.Empty;
        if (normalizado.Length == 9 && normalizado[5] == '-')
            normalizado = normalizado.Remove(5, 1);

        return normalizado.Length == 8 && normalizado.All(char.IsAsciiDigit);
    }

    private record ViaCepResposta(
        string? Cep, string? Logradouro, string? Complemento, string? Bairro,
        string? Localidade, string? Uf, JsonElement Erro);
}
