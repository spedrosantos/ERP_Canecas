namespace MS_Cadastro.Services;

public enum SituacaoConsulta
{
    Encontrado,
    EntradaInvalida,
    NaoEncontrado,
    Indisponivel,
    TempoLimite
}

public record ResultadoConsulta<T>(SituacaoConsulta Situacao, T? Dados = null) where T : class;
