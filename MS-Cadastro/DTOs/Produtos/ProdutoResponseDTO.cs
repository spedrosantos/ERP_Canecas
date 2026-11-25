namespace MS_Cadastro.DTOs.Produtos;

public record ProdutoResponseDTO(
    long Id,
    long CategoriaId,
    string Categoria,
    long UnidadeMedidaId,
    string UnidadeMedida,
    string Sku,
    string Nome,
    string? Descricao,
    string Tipo,
    decimal PrecoCusto,
    decimal PrecoVenda,
    decimal EstoqueMinimo,
    bool Ativo,
    DateTime CriadoEm,
    DateTime? AtualizadoEm
);

public record ProdutosPaginadosResponseDTO(
    List<ProdutoResponseDTO> Itens,
    long Total,
    int Pagina,
    int TamanhoPagina
);