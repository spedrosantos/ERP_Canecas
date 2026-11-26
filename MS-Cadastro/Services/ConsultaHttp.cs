using System.Net;
using System.Text.Json;

namespace MS_Cadastro.Services;

internal static class ConsultaHttp
{
    internal static async Task<ResultadoConsulta<T>> ConsultarAsync<T>(
        HttpClient http, string caminho, CancellationToken cancellationToken) where T : class
    {
        try
        {
            using var resposta = await http.GetAsync(caminho, cancellationToken);
            if (resposta.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = resposta.Headers.RetryAfter;
                var espera = retryAfter?.Delta ?? retryAfter?.Date - DateTimeOffset.UtcNow;
                int? segundos = espera is { TotalSeconds: > 0 } prazo
                    ? (int)Math.Min(int.MaxValue, Math.Ceiling(prazo.TotalSeconds))
                    : null;
                return new(SituacaoConsulta.LimiteDeConsultas, TentarAposSegundos: segundos);
            }
            if (resposta.StatusCode == HttpStatusCode.NotFound)
                return new(SituacaoConsulta.NaoEncontrado);
            if (resposta.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout)
                return new(SituacaoConsulta.TempoLimite);
            if (!resposta.IsSuccessStatusCode)
                return new(SituacaoConsulta.Indisponivel);

            var dados = await resposta.Content.ReadFromJsonAsync<T>(cancellationToken);
            return dados is null ? new(SituacaoConsulta.Indisponivel) : new(SituacaoConsulta.Encontrado, dados);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(SituacaoConsulta.TempoLimite);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            return new(SituacaoConsulta.Indisponivel);
        }
    }
}
