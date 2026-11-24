using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace MS_Cadastro.Authorization;

public sealed class ConsultaCadastralRequirement : IAuthorizationRequirement
{
    public const string CodigoFeature = "CADASTRO_CONSULTAR";
}

public sealed class ConsultaCadastralAuthorizationHandler(
    IHttpContextAccessor contexto, IHttpClientFactory httpClientFactory)
    : AuthorizationHandler<ConsultaCadastralRequirement>
{
    public const string ClienteIdentidade = "Identidade";

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ConsultaCadastralRequirement requirement)
    {
        var requisicao = contexto.HttpContext;
        if (requisicao is null || context.User.Identity?.IsAuthenticated != true)
            return;

        var token = await requisicao.GetTokenAsync(JwtBearerDefaults.AuthenticationScheme, "access_token");
        using var cliente = httpClientFactory.CreateClient(ClienteIdentidade);
        if (string.IsNullOrWhiteSpace(token) || cliente.BaseAddress is null)
            return;

        using var mensagem = new HttpRequestMessage(HttpMethod.Get, "api/usuarios/me/features");
        mensagem.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var cancellationToken = requisicao.RequestAborted;
        try
        {
            using var resposta = await cliente.SendAsync(mensagem, cancellationToken);
            if (!resposta.IsSuccessStatusCode)
                return;

            var features = await resposta.Content.ReadFromJsonAsync<List<FeatureEfetiva>>(cancellationToken);
            if (features?.Any(feature => feature?.Codigo == ConsultaCadastralRequirement.CodigoFeature) == true)
                context.Succeed(requirement);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // A autorização depende de uma resposta válida do MS-Identidade.
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            // A autorização depende de uma resposta válida do MS-Identidade.
        }
    }

    private record FeatureEfetiva(string? Codigo);
}
