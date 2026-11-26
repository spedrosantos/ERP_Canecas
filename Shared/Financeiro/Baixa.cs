namespace Financeiro;

public sealed class Baixa
{
    public const string PadraoFormaPagamento = "^(PIX|DINHEIRO|CARTAO|BOLETO|TRANSFERENCIA)$";

    public long Id { get; set; }
    public long TituloId { get; set; }
    public decimal Valor { get; set; }
    public string FormaPagamento { get; set; } = string.Empty;
    public long UsuarioId { get; set; }
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? EstornadoEm { get; set; }
    public long? UsuarioEstornoId { get; set; }
    public string? MotivoEstorno { get; set; }
}
