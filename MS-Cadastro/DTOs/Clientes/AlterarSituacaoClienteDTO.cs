using System.ComponentModel.DataAnnotations;

namespace MS_Cadastro.DTOs.Clientes;

public class AlterarSituacaoClienteDTO
{
    [Required]
    public bool? Ativo { get; set; }
}
