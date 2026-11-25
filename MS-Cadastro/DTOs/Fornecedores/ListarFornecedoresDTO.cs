using System.ComponentModel.DataAnnotations;

namespace MS_Cadastro.DTOs.Fornecedores;

public class ListarFornecedoresDTO : IValidatableObject
{
    [StringLength(150)]
    public string? Busca { get; set; }

    [RegularExpression("[FfJj]", ErrorMessage = "Informe F ou J para o tipo de pessoa.")]
    public string? TipoPessoa { get; set; }

    public bool? Ativo { get; set; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; set; } = 1;

    [Range(1, 100)]
    public int TamanhoPagina { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (((long)Pagina - 1) * TamanhoPagina > int.MaxValue)
            yield return new ValidationResult("Página fora do intervalo suportado.", [nameof(Pagina)]);
    }
}
