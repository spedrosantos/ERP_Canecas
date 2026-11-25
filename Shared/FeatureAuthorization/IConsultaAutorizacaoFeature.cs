namespace FeatureAuthorization;

public interface IConsultaAutorizacaoFeature
{
    Task<ResultadoAutorizacaoFeature> VerificarAsync(
        string token, string codigoFeature, CancellationToken cancellationToken = default);

    Task<ResultadoAutorizacaoFeature> VerificarSessaoAsync(
        string token, CancellationToken cancellationToken = default);
}

public enum ResultadoAutorizacaoFeature
{
    Negado,
    Permitido,
    TokenInvalido,
    Indisponivel
}
