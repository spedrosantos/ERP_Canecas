namespace MS_Venda.Entities;

public class Pedido
{
    public const string Pendente = "PENDENTE";
    public const string Confirmado = "CONFIRMADO";
    public const string Cancelado = "CANCELADO";

    public long Id { get; set; }
    public long ClienteId { get; set; }
    public long UsuarioId { get; set; }
    public string Situacao { get; set; } = Pendente;
    public decimal ValorTotal { get; set; }
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public DateTime? ConfirmadoEm { get; set; }
    public DateTime? CanceladoEm { get; set; }
    public List<PedidoItem> Itens { get; } = [];
}
