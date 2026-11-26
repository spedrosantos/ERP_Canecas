using System.ComponentModel.DataAnnotations;

namespace MS_Estoque.DTOs.Movimentacoes;

public class AjustarEstoqueDTO
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long ProdutoId { get; set; }

    [Required]
    [RegularExpression("^(ENTRADA|SAIDA)$")]
    public string Tipo { get; set; } = string.Empty;

    public decimal Quantidade { get; set; }

    public decimal? CustoUnitario { get; set; }

    [Required]
    [MaxLength(500)]
    public string Justificativa { get; set; } = string.Empty;
}
