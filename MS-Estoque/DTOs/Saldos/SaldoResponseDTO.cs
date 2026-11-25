namespace MS_Estoque.DTOs.Saldos;

public record SaldoResponseDTO(
    long ProdutoId,
    string NomeProduto,
    string UnidadeMedida,
    decimal Quantidade,
    decimal EstoqueMinimo,
    decimal CustoMedio,
    DateTime? AtualizadoEm
);
