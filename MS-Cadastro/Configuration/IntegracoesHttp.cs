using MS_Cadastro.Authorization;
using MS_Cadastro.Services;
using MS_Cadastro.Services.Interfaces;

namespace MS_Cadastro.Configuration;

public static class IntegracoesHttp
{
    public static IServiceCollection AdicionarIntegracoesHttp(
        this IServiceCollection services, IConfiguration config, bool desenvolvimento)
    {
        Configurar(services.AddHttpClient<IConsultaCepService, ConsultaCepService>(),
            config.GetSection("Integracoes:ViaCep"));
        Configurar(services.AddHttpClient<IConsultaCnpjService, ConsultaCnpjService>(),
            config.GetSection("Integracoes:BrasilApi"));
        Configurar(services.AddHttpClient(ConsultaCadastralAuthorizationHandler.ClienteIdentidade),
            config.GetSection("Integracoes:Identidade"), identidade: true, permitirLoopback: desenvolvimento);

        return services;
    }

    private static void Configurar(
        IHttpClientBuilder builder, IConfigurationSection config,
        bool identidade = false, bool permitirLoopback = false)
    {
        var url = config["BaseUrl"];
        var timeout = config.GetValue<int>("TimeoutSegundos");
        if (timeout is < 1 or > 60)
            throw new InvalidOperationException($"Timeout inválido em {config.Path}.");

        Uri? endereco = null;
        if (!identidade || !string.IsNullOrWhiteSpace(url))
        {
            if (!Uri.TryCreate(url?.TrimEnd('/') + "/", UriKind.Absolute, out endereco)
                || endereco.UserInfo.Length > 0 || endereco.Query.Length > 0 || endereco.Fragment.Length > 0
                || (endereco.Scheme != Uri.UriSchemeHttps
                    && !(permitirLoopback && endereco.IsLoopback && endereco.Scheme == Uri.UriSchemeHttp)))
                throw new InvalidOperationException($"Endereço inválido em {config.Path}.");
        }

        builder.ConfigureHttpClient(cliente =>
        {
            cliente.BaseAddress = endereco;
            cliente.Timeout = TimeSpan.FromSeconds(timeout);
            cliente.MaxResponseContentBufferSize = 1024 * 1024;
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false
        })
        .RemoveAllLoggers();
    }
}
