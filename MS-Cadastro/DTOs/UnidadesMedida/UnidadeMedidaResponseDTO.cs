namespace MS_Cadastro.DTOs.UnidadesMedida;

public record UnidadeMedidaResponseDTO(
    long Id,
    string Sigla,
    string Descricao,
    bool Ativo
);