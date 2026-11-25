using System.ComponentModel.DataAnnotations;

namespace MS_Cadastro.DTOs.UnidadesMedida;

public class SalvarUnidadeMedidaDTO
{
    [Required]
    [MaxLength(10)]
    public string Sigla { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Descricao { get; set; } = string.Empty;
}