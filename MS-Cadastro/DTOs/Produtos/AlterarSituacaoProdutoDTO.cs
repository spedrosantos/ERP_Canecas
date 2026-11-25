using System.ComponentModel.DataAnnotations;

namespace MS_Cadastro.DTOs.Produtos;

public class AlterarSituacaoProdutoDTO
{
    [Required]
    public bool? Ativo { get; set; }
}