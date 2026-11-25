using System.ComponentModel.DataAnnotations;

namespace MS_Cadastro.DTOs.Produtos;

public class SalvarProdutoDTO
{
    [Range(1, long.MaxValue)]
    public long CategoriaId { get; set; }

    [Range(1, long.MaxValue)]
    public long UnidadeMedidaId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    [Required]
    [RegularExpression("^(ACABADO|MATERIA_PRIMA|REVENDA)$")]
    public string Tipo { get; set; } = string.Empty;
}