using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace FeatureAuthorization;

public sealed record FeatureRemotaRequirement(string Codigo, string Esquema = "Bearer")
    : IAuthorizationRequirement;

internal sealed class FeatureRemotaHandler(
    IConsultaAutorizacaoFeature cliente, IHttpContextAccessor accessor)
    : AuthorizationHandler<FeatureRemotaRequirement>
{
    internal const string TokenInvalido = nameof(ResultadoAutorizacaoFeature.TokenInvalido);
    internal const string Indisponivel = nameof(ResultadoAutorizacaoFeature.Indisponivel);

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, FeatureRemotaRequirement requirement)
    {
        var http = context.Resource as HttpContext ?? accessor.HttpContext;
        if (http is null || context.User.Identity?.IsAuthenticated != true)
            return;

        var token = await http.GetTokenAsync(requirement.Esquema, "access_token");
        var resultado = token is null
            ? ResultadoAutorizacaoFeature.TokenInvalido
            : await cliente.VerificarAsync(token, requirement.Codigo, http.RequestAborted);

        switch (resultado)
        {
            case ResultadoAutorizacaoFeature.Permitido:
                context.Succeed(requirement);
                break;
            case ResultadoAutorizacaoFeature.TokenInvalido:
                context.Fail(new AuthorizationFailureReason(this, TokenInvalido));
                break;
            case ResultadoAutorizacaoFeature.Indisponivel:
                context.Fail(new AuthorizationFailureReason(this, Indisponivel));
                break;
        }
    }
}

internal sealed class FeatureRemotaResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _padrao = new();

    public async Task HandleAsync(
        RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var falhas = authorizeResult.AuthorizationFailure?.FailureReasons
            .Where(falha => falha.Handler is FeatureRemotaHandler).Select(falha => falha.Message).ToArray();

        if (falhas?.Contains(FeatureRemotaHandler.TokenInvalido) == true)
        {
            await _padrao.HandleAsync(next, context, policy, PolicyAuthorizationResult.Challenge());
            return;
        }

        if (falhas?.Contains(FeatureRemotaHandler.Indisponivel) == true)
        {
            await Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Serviço de autorização indisponível.").ExecuteAsync(context);
            return;
        }

        await _padrao.HandleAsync(next, context, policy, authorizeResult);
    }
}
