using MS_Compra.DTOs.Pedidos;
using MS_Compra.Entities;

namespace MS_Compra.Repositories.Interfaces;

public interface IPedidoRepository
{
    Task<DashboardCompraDTO> ObterDashboardAsync(DateOnly inicio, DateOnly fim, CancellationToken ct);
    Task<PaginadoResponseDTO<PedidoResponseDTO>> ListarAsync(ListarPedidosDTO filtro, CancellationToken ct);
    Task<Pedido?> ObterAsync(long id, CancellationToken ct);
    Task<Pedido?> BloquearAsync(long id, CancellationToken ct);
    Task<bool> BloquearFornecedorAtivoAsync(long id, CancellationToken ct);
    Task<string?> ObterNomeFornecedorAsync(long id, CancellationToken ct);
    Task<int> BloquearProdutosAtivosAsync(long[] ids, CancellationToken ct);
    Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarFornecedoresAsync(ListarCadastrosDTO filtro, CancellationToken ct);
    Task<PaginadoResponseDTO<CadastroAtivoResponseDTO>> ListarProdutosAsync(ListarCadastrosDTO filtro, CancellationToken ct);
    Task<Dictionary<long, CadastroAtivoResponseDTO>> ObterProdutosAsync(long[] ids, CancellationToken ct);
    void Adicionar(Pedido pedido);
    void RemoverItem(PedidoItem item);
}
