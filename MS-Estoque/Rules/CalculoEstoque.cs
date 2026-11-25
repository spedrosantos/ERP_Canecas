using System.ComponentModel.DataAnnotations;
using MS_Estoque.Entities;

namespace MS_Estoque.Rules;

public static class CalculoEstoque
{
    public static void RegistrarEntrada(
        Saldo saldo,
        decimal quantidade,
        decimal custoUnitario)
    {
        ValidarQuantidade(quantidade);

        if (custoUnitario < 0 ||
            custoUnitario > 99999999999.9999m ||
            decimal.Round(custoUnitario, 4) != custoUnitario)
        {
            throw new ValidationException("Custo unitário inválido.");
        }

        var novaQuantidade = saldo.Quantidade + quantidade;

        if (novaQuantidade > 999999999999.999m)
            throw new ValidationException("Limite de estoque excedido.");

        var valorAtual = saldo.Quantidade * saldo.CustoMedio;
        var valorEntrada = quantidade * custoUnitario;

        saldo.CustoMedio = Math.Round(
            (valorAtual + valorEntrada) / novaQuantidade,
            4,
            MidpointRounding.AwayFromZero);

        saldo.Quantidade = novaQuantidade;
        saldo.AtualizadoEm = DateTime.UtcNow;
    }

    public static decimal RegistrarSaida(
        Saldo saldo,
        decimal quantidade)
    {
        ValidarQuantidade(quantidade);

        if (quantidade > saldo.Quantidade)
            throw new ValidationException("Saldo insuficiente.");

        var custoUnitario = saldo.CustoMedio;

        saldo.Quantidade -= quantidade;

        if (saldo.Quantidade == 0)
            saldo.CustoMedio = 0;

        saldo.AtualizadoEm = DateTime.UtcNow;

        return custoUnitario;
    }

    private static void ValidarQuantidade(decimal quantidade)
    {
        if (quantidade <= 0 ||
            quantidade > 999999999999.999m ||
            decimal.Round(quantidade, 3) != quantidade)
        {
            throw new ValidationException("Quantidade inválida.");
        }
    }
}
