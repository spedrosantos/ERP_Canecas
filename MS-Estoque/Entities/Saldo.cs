namespace MS_Estoque.Entities;

public class Saldo
{
    public long ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public decimal CustoMedio { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
