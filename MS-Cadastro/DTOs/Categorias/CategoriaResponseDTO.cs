namespace MS_Cadastro.DTOs.Categorias;

public record CategoriaResponseDTO(
    long Id,
    string Nome,
    bool Ativo
);