using System.ComponentModel.DataAnnotations;

namespace MS_Venda.DTOs.Pedidos;

public class SalvarPedidoDTO
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long ClienteId { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }

    [Required, MinLength(1)]
    public List<SalvarPedidoItemDTO> Itens { get; set; } = [];
}

public class SalvarPedidoItemDTO
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long ProdutoId { get; set; }
    public decimal Quantidade { get; set; }

    [Required]
    public decimal? PrecoUnitario { get; set; }
}
