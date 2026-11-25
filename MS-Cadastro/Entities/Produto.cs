namespace MS_Cadastro.Entities;

public class Produto
{
    public long Id { get; set; }
    public long CategoriaId { get; set; }
    public long UnidadeMedidaId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal PrecoCusto { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }

    public Categoria Categoria { get; set; } = null!;
    public UnidadeMedida UnidadeMedida { get; set; } = null!;
}