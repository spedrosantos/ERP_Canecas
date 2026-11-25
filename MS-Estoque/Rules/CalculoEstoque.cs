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
    public static decimal RegistrarEstorno(
    Saldo saldo,
    MovimentacaoEstoque original)
    {
        ValidarQuantidade(original.Quantidade);

        if (original.Tipo == "SAIDA")
        {
            RegistrarEntrada(
                saldo,
                original.Quantidade,
                original.CustoUnitario);

            return original.CustoUnitario;
        }

        if (original.Tipo != "ENTRADA")
            throw new ValidationException(
                "Tipo de movimentação inválido.");

        if (original.Quantidade > saldo.Quantidade)
            throw new ValidationException(
                "Saldo insuficiente para realizar o estorno.");

        var valorAtual = saldo.Quantidade * saldo.CustoMedio;

        var valorEstorno =
            original.Quantidade * original.CustoUnitario;

        var novaQuantidade =
            saldo.Quantidade - original.Quantidade;

        var novoValor = valorAtual - valorEstorno;

        if (novaQuantidade > 0 && novoValor < 0)
            throw new ValidationException(
                "O estorno resultaria em um custo de estoque negativo.");

        saldo.Quantidade = novaQuantidade;

        saldo.CustoMedio = novaQuantidade == 0
            ? 0
            : Math.Round(
                novoValor / novaQuantidade,
                4,
                MidpointRounding.AwayFromZero);

        if (saldo.CustoMedio > 99999999999.9999m)
            throw new ValidationException(
                "Limite de custo médio excedido.");

        saldo.AtualizadoEm = DateTime.UtcNow;

        return original.CustoUnitario;
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
