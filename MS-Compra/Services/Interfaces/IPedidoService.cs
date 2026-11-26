using MS_Compra.DTOs.Pedidos;

namespace MS_Compra.Services.Interfaces;

public interface IPedidoService
{
    Task<PaginadoResponseDTO<PedidoResponseDTO>> ListarAsync(ListarPedidosDTO filtro, CancellationToken ct);
    Task<PedidoDetalheResponseDTO?> ObterAsync(long id, CancellationToken ct);
    Task<PedidoDetalheResponseDTO> CadastrarAsync(SalvarPedidoDTO dto, CancellationToken ct);
    Task<PedidoDetalheResponseDTO?> EditarAsync(long id, SalvarPedidoDTO dto, CancellationToken ct);
    Task<PedidoDetalheResponseDTO?> ReceberAsync(long id, ReceberPedidoDTO? dto, CancellationToken ct);
    Task<PedidoDetalheResponseDTO?> CancelarAsync(long id, CancellationToken ct);
    Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarFornecedoresAsync(ListarCadastrosDTO filtro, CancellationToken ct);
    Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarProdutosAsync(ListarCadastrosDTO filtro, CancellationToken ct);
}
