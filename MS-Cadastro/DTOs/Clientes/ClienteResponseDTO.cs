namespace MS_Cadastro.DTOs.Clientes;

public record ClienteResponseDTO(
    long Id, string TipoPessoa, string Nome, string? NomeFantasia,
    string CpfCnpj, string? Telefone, string? Email, string? Cep,
    string? Logradouro, string? Numero, string? Complemento, string? Bairro,
    string? Cidade, string? Uf, bool Ativo, DateTime CriadoEm, DateTime? AtualizadoEm);

public record ClientesPaginadosResponseDTO(
    IReadOnlyList<ClienteResponseDTO> Itens, long Total, int Pagina, int TamanhoPagina);
