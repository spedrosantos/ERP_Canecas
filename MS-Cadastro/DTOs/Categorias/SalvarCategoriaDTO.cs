using System.ComponentModel.DataAnnotations;

namespace MS_Cadastro.DTOs.Categorias;

public class SalvarCategoriaDTO
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;
}