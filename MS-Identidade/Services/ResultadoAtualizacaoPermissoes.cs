namespace MS_Identidade.Services;

public enum ResultadoAtualizacaoPermissoes
{
    Atualizado,
    UsuarioInexistente,
    AlteracaoPropria,
    SemAutoridade,
    FeaturesInvalidas,
    HierarquiaInvalida,
    UltimoGerenciador,
    AlteracaoSimultanea
}
