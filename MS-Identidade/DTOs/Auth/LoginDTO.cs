using System.ComponentModel.DataAnnotations;

namespace MS_Identidade.DTOs.Auth;

public class LoginDTO
{
    [Range(1, long.MaxValue)]
    public long EmpresaId { get; set; }

    [Range(1, long.MaxValue)]
    public long UsuarioId { get; set; }

    [Required]
    public string Senha { get; set; } = string.Empty;
}