using System.Net;
using System.Net.Http.Headers;

namespace FeatureAuthorization;

internal sealed class ClienteAutorizacaoFeature(IHttpClientFactory httpClientFactory)
    : IConsultaAutorizacaoFeature
{
    internal const string NomeCliente = "Autorizacao.MS-Identidade";

    public async Task<ResultadoAutorizacaoFeature> VerificarAsync(
        string token, string codigoFeature, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return ResultadoAutorizacaoFeature.TokenInvalido;

        if (string.IsNullOrWhiteSpace(codigoFeature) || codigoFeature.Length > 100)
            return ResultadoAutorizacaoFeature.Negado;

        using var cliente = httpClientFactory.CreateClient(NomeCliente);
        if (cliente.BaseAddress is null)
            return ResultadoAutorizacaoFeature.Indisponivel;

        using var requisicao = new HttpRequestMessage(HttpMethod.Get,
            $"api/usuarios/me/features/verificar?codigo={Uri.EscapeDataString(codigoFeature)}");
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var resposta = await cliente.SendAsync(
                requisicao, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return resposta.StatusCode switch
            {
                HttpStatusCode.NoContent => ResultadoAutorizacaoFeature.Permitido,
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
