namespace MS_Cadastro.DTOs.Consultas;

public record EnderecoConsultaDTO(
    string? Cep, string? Logradouro, string? Numero, string? Complemento,
    string? Bairro, string? Cidade, string? Uf);
