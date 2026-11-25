namespace MS_Cadastro.Entities.Interfaces;

public interface IPessoaCadastro : IDocumentoFiscal
{
    long Id { get; set; }
    string Nome { get; set; }
    string? NomeFantasia { get; set; }
    string? Telefone { get; set; }
    string? Email { get; set; }
    string? Cep { get; set; }
    string? Logradouro { get; set; }
    string? Numero { get; set; }
    string? Complemento { get; set; }
    string? Bairro { get; set; }
    string? Cidade { get; set; }
    string? Uf { get; set; }
    bool Ativo { get; set; }
    DateTime CriadoEm { get; set; }
    DateTime? AtualizadoEm { get; set; }
}
