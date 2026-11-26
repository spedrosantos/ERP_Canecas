namespace MS_Venda.DTOs.Pedidos;

public record PedidoResponseDTO(
    long Id, long ClienteId, long UsuarioId, string Situacao,
    decimal ValorTotal, string? Observacao, DateTime CriadoEm,
    DateTime? AtualizadoEm, DateTime? ConfirmadoEm, DateTime? CanceladoEm);

public record PedidoDetalheResponseDTO(
    PedidoResponseDTO Pedido, IReadOnlyList<PedidoItemResponseDTO> Itens, string? NomeCliente);

public record PedidoItemResponseDTO(
    long Id, long ProdutoId, decimal Quantidade, decimal PrecoUnitario, decimal? CustoUnitario,
    string? NomeProduto, string? UnidadeMedida);

public record CadastroAtivoResponseDTO(long Id, string Nome, string? UnidadeMedida);

public record DisponibilidadeProdutoResponseDTO(
    long ProdutoId, string NomeProduto, string UnidadeMedida, decimal QuantidadeDisponivel);

public record PaginadoResponseDTO<T>(
    IReadOnlyList<T> Itens, long Total, int Pagina, int TamanhoPagina);
