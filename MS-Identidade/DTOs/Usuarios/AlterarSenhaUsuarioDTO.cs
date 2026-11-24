using System.ComponentModel.DataAnnotations;

namespace MS_Identidade.DTOs.Usuarios;

public class AlterarSenhaUsuarioDTO
{
    [Required]
    [MinLength(8)]
    public string Senha { get; set; } = string.Empty;
}
