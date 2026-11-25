using System.ComponentModel.DataAnnotations;

namespace MS_Estoque.DTOs.Movimentacoes;

public class EstornarMovimentacaoDTO
{
    [Required]
    [MaxLength(500)]
    public string Justificativa { get; set; } = string.Empty;
}
