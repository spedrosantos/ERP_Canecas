using System.ComponentModel.DataAnnotations;
using Financeiro;

namespace MS_Financeiro.DTOs;

public static class SituacaoTitulo
{
    public const string Pendente = "PENDENTE";
    public const string Parcial = "PARCIAL";
    public const string Quitado = "QUITADO";
    public const string Vencido = "VENCIDO";
    public const string Cancelado = "CANCELADO";
    public const string Padrao = "^(PENDENTE|PARCIAL|QUITADO|VENCIDO|CANCELADO)$";
}

public class FiltroTitulosDTO
{
    [RegularExpression("^(PAGAR|RECEBER)$")]
    public string? Tipo { get; set; }

    [RegularExpression(SituacaoTitulo.Padrao)]
    public string? Situacao { get; set; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? FornecedorId { get; set; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? ClienteId { get; set; }

    public DateOnly? VencimentoInicio { get; set; }
    public DateOnly? VencimentoFim { get; set; }
}

public class ListarTitulosDTO : FiltroTitulosDTO
{
    [Range(1, int.MaxValue)]
    public int Pagina { get; set; } = 1;

    [Range(1, 100)]
    public int TamanhoPagina { get; set; } = 20;
}

public class RegistrarBaixaDTO
{
    [Range(typeof(decimal), "0.01", "99999999999999999.99")]
    public decimal Valor { get; set; }

    [Required, RegularExpression(Baixa.PadraoFormaPagamento)]
    public string FormaPagamento { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Observacao { get; set; }
}

public class EstornarBaixaDTO
{
    [Required, StringLength(500)]
    public string Motivo { get; set; } = string.Empty;
}

public record TituloResponseDTO(
    long Id, string Tipo, long? CompraId, long? VendaId,
    long? FornecedorId, string? NomeFornecedor, long? ClienteId, string? NomeCliente,
    decimal Valor, decimal ValorBaixado, decimal Saldo, string Situacao,
    DateOnly Vencimento, long UsuarioId, DateTime CriadoEm, DateTime? CanceladoEm);

public record TitulosPaginadosResponseDTO(
    IReadOnlyList<TituloResponseDTO> Itens, long Total, int Pagina, int TamanhoPagina);

public record TituloDetalheResponseDTO(TituloResponseDTO Titulo, IReadOnlyList<BaixaResponseDTO> Baixas);

public record BaixaResponseDTO(
    long Id, long TituloId, decimal Valor, string FormaPagamento, long UsuarioId,
    string? Observacao, DateTime CriadoEm, DateTime? EstornadoEm,
    long? UsuarioEstornoId, string? MotivoEstorno);

public record IndicadorFinanceiroDTO(
    long TotalTitulos, decimal ValorTotal, decimal ValorBaixado, decimal Saldo, decimal SaldoVencido);

public record IndicadoresFinanceirosResponseDTO(
    IndicadorFinanceiroDTO ContasAPagar, IndicadorFinanceiroDTO ContasAReceber);
