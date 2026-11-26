using System.ComponentModel.DataAnnotations;

namespace MS_Venda.DTOs.Pedidos;

public class ListarPedidosDTO
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long? ClienteId { get; set; }

    [RegularExpression("^(PENDENTE|CONFIRMADO|CANCELADO)$")]
    public string? Situacao { get; set; }

    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; set; } = 1;

    [Range(1, 100)]
    public int TamanhoPagina { get; set; } = 20;
}

public class ListarCadastrosDTO
{
    [StringLength(150)]
    public string? Busca { get; set; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; set; } = 1;

    [Range(1, 100)]
    public int TamanhoPagina { get; set; } = 20;
}
