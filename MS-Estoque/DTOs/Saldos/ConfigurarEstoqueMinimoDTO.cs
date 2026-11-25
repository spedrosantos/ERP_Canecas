using System.ComponentModel.DataAnnotations;

namespace MS_Estoque.DTOs.Saldos;

public class ConfigurarEstoqueMinimoDTO
{
    [Required]
    [Range(typeof(decimal), "0", "999999999999.999")]
    public decimal? EstoqueMinimo { get; set; }
}
