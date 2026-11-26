using MS_Venda.DTOs.Pedidos;
using MS_Venda.Entities;
using MS_Estoque.Entities;

namespace MS_Venda.Repositories.Interfaces;

public interface IPedidoRepository
{
    Task<DashboardVendaDTO> ObterDashboardAsync(DateOnly inicio, DateOnly fim, CancellationToken ct);
    Task<PaginadoResponseDTO<PedidoResponseDTO>> ListarAsync(ListarPedidosDTO filtro, CancellationToken ct);
    Task<Pedido?> ObterAsync(long id, CancellationToken ct);
    Task<Pedido?> BloquearAsync(long id, CancellationToken ct);
    Task<bool> BloquearClienteAtivoAsync(long id, CancellationToken ct);
    Task<string?> ObterNomeClienteAsync(long id, CancellationToken ct);
    Task<int> BloquearProdutosAtivosAsync(long[] ids, CancellationToken ct);
    Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarClientesAsync(ListarCadastrosDTO filtro, CancellationToken ct);
    Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarProdutosAsync(ListarCadastrosDTO filtro, CancellationToken ct);
    Task<Dictionary<long, CadastroAtivoResponseDTO>> ObterProdutosAsync(long[] ids, CancellationToken ct);
    Task<DisponibilidadeProdutoResponseDTO?> ObterDisponibilidadeAsync(long produtoId, CancellationToken ct);
    Task<List<MovimentacaoEstoque>> ListarSaidasDaVendaAsync(long pedidoId, CancellationToken ct);
    void Adicionar(Pedido pedido);
    void RemoverItem(PedidoItem item);
}
