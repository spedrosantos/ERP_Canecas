namespace MS_Cadastro.Entities;
using MS_Cadastro.Entities.Interfaces;

public class Cliente : IPessoaCadastro
{
    public long Id { get; set; }
    public string TipoPessoa { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string CpfCnpj { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}
