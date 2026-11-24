using System.ComponentModel.DataAnnotations;

namespace MS_Identidade.DTOs.Usuarios;

public class AlterarSituacaoUsuarioDTO
{
    [Required]
    public bool? Ativo { get; set; }
}
