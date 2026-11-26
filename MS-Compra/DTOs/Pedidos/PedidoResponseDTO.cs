namespace MS_Compra.DTOs.Pedidos;

public record PedidoResponseDTO(
    long Id, long FornecedorId, long UsuarioId, string Situacao,
    decimal ValorTotal, string? Observacao, DateTime CriadoEm,
    DateTime? AtualizadoEm, DateTime? RecebidoEm);

public record PedidoDetalheResponseDTO(
    PedidoResponseDTO Pedido, IReadOnlyList<PedidoItemResponseDTO> Itens, string? NomeFornecedor);

public record PedidoItemResponseDTO(
    long Id, long ProdutoId, decimal Quantidade, decimal CustoUnitario,
    string? NomeProduto, string? UnidadeMedida);

public record CadastroAtivoResponseDTO(long Id, string Nome, string? UnidadeMedida);

public record PaginadoResponseDTO<T>(
    IReadOnlyList<T> Itens, long Total, int Pagina, int TamanhoPagina);
