using System.ComponentModel.DataAnnotations;

namespace MS_Cadastro.DTOs.Fornecedores;

public class AlterarSituacaoFornecedorDTO
{
    [Required]
    public bool? Ativo { get; set; }
}
