namespace MS_Estoque.DTOs.Saldos;

public record SaldosPaginadosResponseDTO(
    List<SaldoResponseDTO> Itens,
    long Total,
    int Pagina,
    int TamanhoPagina
);
