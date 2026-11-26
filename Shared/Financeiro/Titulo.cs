namespace Financeiro;

public sealed class Titulo
{
    public const string Pagar = "PAGAR";
    public const string Receber = "RECEBER";

    public long Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public long? CompraId { get; set; }
    public long? VendaId { get; set; }
    public decimal Valor { get; set; }
    public DateOnly Vencimento { get; set; }
    public long UsuarioId { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? CanceladoEm { get; set; }
    public List<Baixa> Baixas { get; } = [];
}
