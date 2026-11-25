using System.ComponentModel.DataAnnotations;

namespace MS_Cadastro.DTOs.Produtos;

public class ListarProdutosDTO
{
    [MaxLength(150)]
    public string? Busca { get; set; }

    [Range(1, long.MaxValue)]
    public long? CategoriaId { get; set; }

    [RegularExpression("^(ACABADO|MATERIA_PRIMA|REVENDA)$")]
    public string? Tipo { get; set; }

    public bool? Ativo { get; set; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; set; } = 1;

    [Range(1, 100)]
    public int TamanhoPagina { get; set; } = 20;
}