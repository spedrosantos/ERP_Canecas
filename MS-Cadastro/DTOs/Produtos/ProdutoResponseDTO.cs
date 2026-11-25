namespace MS_Cadastro.DTOs.Produtos;

public record ProdutoResponseDTO(
    long Id,
    long CategoriaId,
    string Categoria,
    long UnidadeMedidaId,
    string UnidadeMedida,
    string Nome,
    string? Descricao,
    string Tipo,
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