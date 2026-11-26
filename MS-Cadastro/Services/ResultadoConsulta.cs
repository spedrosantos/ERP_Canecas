namespace MS_Cadastro.Services;

public enum SituacaoConsulta
{
    Encontrado,
    EntradaInvalida,
    NaoEncontrado,
    Indisponivel,
    TempoLimite,
    LimiteDeConsultas
}

public record ResultadoConsulta<T>(
    SituacaoConsulta Situacao, T? Dados = null, int? TentarAposSegundos = null) where T : class;
