namespace MS_Estoque.DTOs.Movimentacoes;

public record MovimentacoesPaginadasResponseDTO(
    List<MovimentacaoResponseDTO> Itens,
    long Total,
    int Pagina,
    int TamanhoPagina
);
