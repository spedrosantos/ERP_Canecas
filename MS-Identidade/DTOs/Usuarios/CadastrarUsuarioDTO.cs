using System.ComponentModel.DataAnnotations;

namespace MS_Identidade.DTOs.Usuarios;

public class CadastrarUsuarioDTO
{
    
    [Required]
    [StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Senha { get; set; } = string.Empty;
}