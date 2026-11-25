using System.ComponentModel.DataAnnotations;

namespace MS_Cadastro.DTOs.Produtos;

public class SalvarProdutoDTO
{
    [Range(1, long.MaxValue)]
    public long CategoriaId { get; set; }

    [Range(1, long.MaxValue)]
    public long UnidadeMedidaId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    [Required]
    [RegularExpression("^(ACABADO|MATERIA_PRIMA|REVENDA)$")]
    public string Tipo { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "99999999999.9999")]
    public decimal PrecoCusto { get; set; }

    [Range(typeof(decimal), "0", "9999999999999.99")]
    public decimal PrecoVenda { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999")]
    public decimal EstoqueMinimo { get; set; }
}