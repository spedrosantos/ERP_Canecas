using System.ComponentModel.DataAnnotations;

namespace MS_Estoque.DTOs.Movimentacoes;

public class ListarMovimentacoesDTO
{
    public long? ProdutoId { get; set; }

    [RegularExpression("^(ENTRADA|SAIDA)$")]
    public string? Tipo { get; set; }

    [RegularExpression("^(COMPRA|VENDA|AJUSTE|ESTORNO)$")]
    public string? Origem { get; set; }

    public DateOnly? DataInicio { get; set; }

    public DateOnly? DataFim { get; set; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; set; } = 1;

    [Range(1, 100)]
    public int TamanhoPagina { get; set; } = 20;
}
