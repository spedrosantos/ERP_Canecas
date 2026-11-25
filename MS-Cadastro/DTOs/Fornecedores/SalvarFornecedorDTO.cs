using System.ComponentModel.DataAnnotations;
using MS_Cadastro.Validations;

namespace MS_Cadastro.DTOs.Fornecedores;

public class SalvarFornecedorDTO : IValidatableObject
{
    [Required, RegularExpression("[FfJj]", ErrorMessage = "Informe F ou J para o tipo de pessoa.")]
    public string TipoPessoa { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(150)] public string? NomeFantasia { get; set; }

    [Required, StringLength(18)]
    public string CpfCnpj { get; set; } = string.Empty;

    [StringLength(20)] public string? Telefone { get; set; }
    [StringLength(150)] public string? Email { get; set; }
    [StringLength(8)] public string? Cep { get; set; }
    [StringLength(200)] public string? Logradouro { get; set; }
    [StringLength(20)] public string? Numero { get; set; }
    [StringLength(100)] public string? Complemento { get; set; }
    [StringLength(100)] public string? Bairro { get; set; }
    [StringLength(100)] public string? Cidade { get; set; }
    [StringLength(2)] public string? Uf { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var pessoaFisica = string.Equals(TipoPessoa, "F", StringComparison.OrdinalIgnoreCase);
        var documentoValido = pessoaFisica
            ? DocumentoFiscal.TentarNormalizarCpf(CpfCnpj, out _)
            : DocumentoFiscal.TentarNormalizarCnpj(CpfCnpj, out _);

        if (!documentoValido)
            yield return new ValidationResult(
                pessoaFisica ? "Informe um CPF válido." : "Informe um CNPJ válido.",
                [nameof(CpfCnpj)]);
    }
}
