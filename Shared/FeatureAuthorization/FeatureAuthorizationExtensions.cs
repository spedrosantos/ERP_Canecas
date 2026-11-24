using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FeatureAuthorization;

public static class FeatureAuthorizationExtensions
{
    public static IServiceCollection AdicionarAutorizacaoPorFeatures(
        this IServiceCollection services, IConfigurationSection config, bool permitirHttpLocal = false)
    {
        var url = config["BaseUrl"];
        var timeout = config.GetValue<int?>("TimeoutSegundos") ?? 3;
        if (timeout is < 1 or > 60)
            throw new InvalidOperationException($"Timeout inválido em {config.Path}.");

        Uri? endereco = null;
        if (!string.IsNullOrWhiteSpace(url))
        {
            if (!Uri.TryCreate(url.TrimEnd('/') + "/", UriKind.Absolute, out endereco)
                || endereco.UserInfo.Length > 0 || endereco.Query.Length > 0 || endereco.Fragment.Length > 0
                || (endereco.Scheme != Uri.UriSchemeHttps
                    && !(permitirHttpLocal && endereco.IsLoopback && endereco.Scheme == Uri.UriSchemeHttp)))
                throw new InvalidOperationException($"Endereço inválido em {config.Path}.");
        }

        services.AddHttpClient(ClienteAutorizacaoFeature.NomeCliente, cliente =>
        {
            cliente.BaseAddress = endereco;
            cliente.Timeout = TimeSpan.FromSeconds(timeout);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            UseProxy = false
        })
        .RemoveAllLoggers();

        services.AddHttpContextAccessor();
        services.AddScoped<IConsultaAutorizacaoFeature, ClienteAutorizacaoFeature>();
        services.AddScoped<IAuthorizationHandler, FeatureRemotaHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, FeatureRemotaResultHandler>();

        return services;
    }
}
