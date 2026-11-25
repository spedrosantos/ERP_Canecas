using System.Net;
using System.Net.Http.Headers;

namespace FeatureAuthorization;

internal sealed class ClienteAutorizacaoFeature(IHttpClientFactory httpClientFactory)
    : IConsultaAutorizacaoFeature
{
    internal const string NomeCliente = "Autorizacao.MS-Identidade";

    public Task<ResultadoAutorizacaoFeature> VerificarAsync(
        string token, string codigoFeature, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoFeature) || codigoFeature.Length > 100)
            return Task.FromResult(ResultadoAutorizacaoFeature.Negado);

        return EnviarAsync(token, $"api/usuarios/me/features/verificar?codigo={Uri.EscapeDataString(codigoFeature)}",
            HttpStatusCode.NoContent, cancellationToken);
    }

    public Task<ResultadoAutorizacaoFeature> VerificarSessaoAsync(
        string token, CancellationToken cancellationToken = default)
    {
        return EnviarAsync(token, "api/usuarios/me/features", HttpStatusCode.OK, cancellationToken);
    }

    private async Task<ResultadoAutorizacaoFeature> EnviarAsync(
        string token, string caminho, HttpStatusCode sucesso, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return ResultadoAutorizacaoFeature.TokenInvalido;

        using var cliente = httpClientFactory.CreateClient(NomeCliente);
        if (cliente.BaseAddress is null)
            return ResultadoAutorizacaoFeature.Indisponivel;

        using var requisicao = new HttpRequestMessage(HttpMethod.Get, caminho);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var resposta = await cliente.SendAsync(
                requisicao, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return resposta.StatusCode switch
            {
                var status when status == sucesso => ResultadoAutorizacaoFeature.Permitido,
                HttpStatusCode.Unauthorized => ResultadoAutorizacaoFeature.TokenInvalido,
                HttpStatusCode.Forbidden => ResultadoAutorizacaoFeature.Negado,
                _ => ResultadoAutorizacaoFeature.Indisponivel
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ResultadoAutorizacaoFeature.Indisponivel;
        }
        catch (HttpRequestException)
        {
            return ResultadoAutorizacaoFeature.Indisponivel;
        }
    }
}
