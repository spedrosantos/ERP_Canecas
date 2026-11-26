namespace MS_Compra.DTOs.Pedidos;

public record CompraPorPeriodoDTO(DateOnly Mes, decimal Valor);
public record DashboardCompraDTO(decimal TotalCompras, IReadOnlyList<CompraPorPeriodoDTO> Evolucao);
