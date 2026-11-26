namespace MS_Estoque.DTOs.Saldos;

public record SaldoResponseDTO(
    long ProdutoId,
    string NomeProduto,
    string UnidadeMedida,
    bool ProdutoAtivo,
    decimal Quantidade,
    decimal EstoqueMinimo,
    decimal CustoMedio,
    DateTime? AtualizadoEm
);
