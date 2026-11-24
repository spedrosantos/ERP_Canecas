namespace MS_Identidade.Services;

public enum ResultadoAtualizacaoPermissoes
{
    Atualizado,
    UsuarioInexistente,
    SemAutoridade,
    UsuariosInvalidos,
    FeaturesInvalidas,
    HierarquiaInvalida,
    UltimoGerenciador,
    AlteracaoSimultanea
}
