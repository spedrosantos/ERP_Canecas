using MS_Venda.DTOs.Pedidos;

namespace MS_Venda.Services.Interfaces;

public interface IPedidoService
{
    Task<DashboardVendaDTO> ObterDashboardAsync(DateOnly inicio, DateOnly fim, CancellationToken ct);
    Task<PaginadoResponseDTO<PedidoResponseDTO>> ListarAsync(ListarPedidosDTO filtro, CancellationToken ct);
    Task<PedidoDetalheResponseDTO?> ObterAsync(long id, CancellationToken ct);
    Task<PedidoDetalheResponseDTO> CadastrarAsync(SalvarPedidoDTO dto, CancellationToken ct);
    Task<PedidoDetalheResponseDTO?> EditarAsync(long id, SalvarPedidoDTO dto, CancellationToken ct);
    Task<PedidoDetalheResponseDTO?> ConfirmarAsync(long id, ConfirmarPedidoDTO? dto, CancellationToken ct);
    Task<PedidoDetalheResponseDTO?> CancelarAsync(long id, CancellationToken ct);
    Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarClientesAsync(ListarCadastrosDTO filtro, CancellationToken ct);
    Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarProdutosAsync(ListarCadastrosDTO filtro, CancellationToken ct);
    Task<DisponibilidadeProdutoResponseDTO?> ObterDisponibilidadeAsync(long produtoId, CancellationToken ct);
}
