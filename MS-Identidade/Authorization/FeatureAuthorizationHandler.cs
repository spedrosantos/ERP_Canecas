using Microsoft.AspNetCore.Authorization;
using MS_Identidade.Services.Interfaces;

namespace MS_Identidade.Authorization;

public class FeatureAuthorizationHandler
    : AuthorizationHandler<FeatureRequirement>
{
    private readonly IPermissaoService _permissaoService;

    public FeatureAuthorizationHandler(
        IPermissaoService permissaoService)
    {
        _permissaoService = permissaoService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FeatureRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var empresaIdClaim = context.User
            .FindFirst("empresa_id")?.Value;

        var usuarioIdClaim = context.User
            .FindFirst("usuario_id")?.Value;

        if (!long.TryParse(empresaIdClaim, out var empresaId)
            || empresaId <= 0)
            return;

        if (!long.TryParse(usuarioIdClaim, out var usuarioId)
            || usuarioId <= 0)
            return;

        bool permitido;
        if (requirement.Alternativas.Count == 0)
        {
            permitido = await _permissaoService.PossuiPermissaoAsync(
                empresaId, usuarioId, requirement.CodigoFeature);
        }
        else
        {
            var efetivas = await _permissaoService.ListarEfetivasAsync(empresaId, usuarioId);
            var codigos = efetivas?.Select(feature => feature.Codigo).ToHashSet();
            permitido = codigos is not null
                && codigos.Contains(requirement.CodigoFeature)
                && requirement.Alternativas.Any(codigos.Contains);
        }

        if (permitido)
            context.Succeed(requirement);
    }
}
