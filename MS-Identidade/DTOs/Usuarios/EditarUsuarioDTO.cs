using System.ComponentModel.DataAnnotations;

namespace MS_Identidade.DTOs.Usuarios;

public class EditarUsuarioDTO
{
    [Required]
    [StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MinLength(8)]
    [RegularExpression(@"[\s\S]*\S[\s\S]*")]
    public string? Senha { get; set; }
}
