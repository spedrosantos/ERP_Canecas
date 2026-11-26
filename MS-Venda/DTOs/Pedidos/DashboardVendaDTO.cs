namespace MS_Venda.DTOs.Pedidos;

public record VendaPorPeriodoDTO(DateOnly Mes, decimal Valor, long Quantidade, decimal? LucroBruto);
public record ProdutoMaisVendidoDTO(long ProdutoId, string? NomeProduto, decimal Quantidade);
public record VendaRecenteDTO(long Id, long ClienteId, string? NomeCliente, DateTime ConfirmadoEm, decimal Valor);
public record DashboardVendaDTO(
    decimal Faturamento, long VendasConfirmadas, decimal? LucroBruto, decimal? MargemBruta,
    IReadOnlyList<VendaPorPeriodoDTO> Evolucao, IReadOnlyList<ProdutoMaisVendidoDTO> ProdutosMaisVendidos,
    IReadOnlyList<VendaRecenteDTO> UltimasVendas);
