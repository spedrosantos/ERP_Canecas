namespace MS_Estoque.Entities;

public class MovimentacaoEstoque
{
    public long Id { get; set; }
    public long ProdutoId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal CustoUnitario { get; set; }
    public string Origem { get; set; } = string.Empty;
    public long? ReferenciaId { get; set; }
    public long? MovimentacaoOriginalId { get; set; }
    public long UsuarioId { get; set; }
    public string? Justificativa { get; set; }
    public DateTime CriadoEm { get; set; }
}
