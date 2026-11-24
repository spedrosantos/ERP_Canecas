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

        var permitido = await _permissaoService
            .PossuiPermissaoAsync(
                empresaId,
                usuarioId,
                requirement.CodigoFeature);

        if (permitido)
            context.Succeed(requirement);
    }
}