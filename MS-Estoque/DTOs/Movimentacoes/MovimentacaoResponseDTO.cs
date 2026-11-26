namespace MS_Estoque.DTOs.Movimentacoes;

public record MovimentacaoResponseDTO(
    long Id,
    long ProdutoId,
    string Tipo,
    decimal Quantidade,
    decimal CustoUnitario,
    string Origem,
    long? ReferenciaId,
    long? MovimentacaoOriginalId,
    long UsuarioId,
    string? Justificativa,
    DateTime CriadoEm,
    string? NomeProduto = null,
    string? NomeUsuario = null,
    long? EstornoId = null
);
