using System.ComponentModel.DataAnnotations;

namespace MS_Identidade.DTOs.Usuarios;

public class AtualizarPermissoesDTO
{
    [Required]
    public List<long>? FeatureIds { get; set; }
}
