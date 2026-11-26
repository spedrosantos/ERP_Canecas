using MS_Cadastro.Services;
using MS_Cadastro.Services.Interfaces;

namespace MS_Cadastro.Configuration;

public static class IntegracoesHttp
{
    public static IServiceCollection AdicionarIntegracoesHttp(
        this IServiceCollection services, IConfiguration config)
    {
        Configurar(services.AddHttpClient<IConsultaCepService, ConsultaCepService>(),
            config.GetSection("Integracoes:ViaCep"));
        Configurar(services.AddHttpClient<IConsultaCnpjService, ConsultaCnpjService>(),
            config.GetSection("Integracoes:BrasilApi"));
        return services;
    }

    private static void Configurar(IHttpClientBuilder builder, IConfigurationSection config)
    {
        var url = config["BaseUrl"];
        var timeout = config.GetValue<int>("TimeoutSegundos");
        if (timeout is < 1 or > 60)
            throw new InvalidOperationException($"Timeout inválido em {config.Path}.");

        if (!Uri.TryCreate(url?.TrimEnd('/') + "/", UriKind.Absolute, out var endereco)
            || endereco.Scheme != Uri.UriSchemeHttps || endereco.UserInfo.Length > 0
            || endereco.Query.Length > 0 || endereco.Fragment.Length > 0)
            throw new InvalidOperationException($"Endereço inválido em {config.Path}.");

        builder.ConfigureHttpClient(cliente =>
        {
            cliente.BaseAddress = endereco;
            cliente.DefaultRequestHeaders.UserAgent.ParseAdd("CanecasERP/1.0");
            cliente.DefaultRequestHeaders.Accept.ParseAdd("application/json");
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
