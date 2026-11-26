using System.ComponentModel.DataAnnotations;

namespace MS_Compra.DTOs.Pedidos;

public class SalvarPedidoDTO
{
    public long FornecedorId { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }

    [Required, MinLength(1)]
    public List<SalvarPedidoItemDTO> Itens { get; set; } = [];
}

public class SalvarPedidoItemDTO
{
    public long ProdutoId { get; set; }
    public decimal Quantidade { get; set; }

    [Required]
    public decimal? CustoUnitario { get; set; }
}
