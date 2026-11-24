using System.ComponentModel.DataAnnotations;

namespace MS_Identidade.DTOs.Usuarios;

public class AtualizarPermissoesEmLoteDTO
{
    [Required, MinLength(1)]
    public List<long> UsuarioIds { get; set; } = [];

    [Required]
    public List<long> ConcederIds { get; set; } = [];

    [Required]
    public List<long> RevogarIds { get; set; } = [];
}
