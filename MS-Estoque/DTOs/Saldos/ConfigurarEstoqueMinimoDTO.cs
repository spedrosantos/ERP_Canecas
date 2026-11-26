using System.ComponentModel.DataAnnotations;

namespace MS_Estoque.DTOs.Saldos;

public class ConfigurarEstoqueMinimoDTO
{
    [Required]
    public decimal? EstoqueMinimo { get; set; }
}
