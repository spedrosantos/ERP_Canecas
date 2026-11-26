namespace MS_Compra.DTOs.Pedidos;

public record CompraMensalDTO(DateOnly Mes, decimal Valor);
public record DashboardCompraDTO(decimal TotalCompras, IReadOnlyList<CompraMensalDTO> Evolucao);
