namespace MS_Cadastro.DTOs.Consultas;

public record EmpresaConsultaDTO(
    string Cnpj, string RazaoSocial, string? NomeFantasia, EnderecoConsultaDTO Endereco);
