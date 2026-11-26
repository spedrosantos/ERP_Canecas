namespace MS_Compra.Entities;

public class PedidoItem
{
    public long Id { get; set; }
    public long PedidoId { get; set; }
    public long ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal CustoUnitario { get; set; }
}
