namespace MS_Cadastro.Validations;

public static class DocumentoFiscal
{
    public static bool TentarNormalizarCpf(string? valor, out string cpf)
    {
        cpf = Normalizar(valor);
        return cpf.Length == 11
            && cpf.All(char.IsAsciiDigit)
            && !CaracteresIguais(cpf)
            && cpf[9] - '0' == CalcularDigitoCpf(cpf.AsSpan(0, 9))
            && cpf[10] - '0' == CalcularDigitoCpf(cpf.AsSpan(0, 10));
    }

    public static bool TentarNormalizarCnpj(string? valor, out string cnpj)
    {
        cnpj = Normalizar(valor);
        return cnpj.Length == 14
            && char.IsAsciiDigit(cnpj[12])
            && char.IsAsciiDigit(cnpj[13])
            && !CaracteresIguais(cnpj)
            && cnpj[12] - '0' == CalcularDigitoCnpj(cnpj.AsSpan(0, 12))
            && cnpj[13] - '0' == CalcularDigitoCnpj(cnpj.AsSpan(0, 13));
    }

    private static bool CaracteresIguais(string documento)
    {
        return documento.All(c => c == documento[0]);
    }

    private static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        var documento = new System.Text.StringBuilder(14);
        foreach (var caractere in valor.Trim())
        {
            if (caractere is '.' or '-' or '/')
                continue;

            if (!char.IsAsciiLetterOrDigit(caractere) || documento.Length == 14)
                return string.Empty;

            documento.Append(char.ToUpperInvariant(caractere));
        }

        return documento.ToString();
    }

    private static int CalcularDigitoCpf(ReadOnlySpan<char> baseCpf)
    {
        var soma = 0;
        for (var i = 0; i < baseCpf.Length; i++)
            soma += (baseCpf[i] - '0') * (baseCpf.Length + 1 - i);

        return DigitoModulo11(soma);
    }

    private static int CalcularDigitoCnpj(ReadOnlySpan<char> baseCnpj)
    {
        var soma = 0;
        for (var i = 0; i < baseCnpj.Length; i++)
            soma += (baseCnpj[i] - '0') * (2 + (baseCnpj.Length - 1 - i) % 8);

        return DigitoModulo11(soma);
    }

    private static int DigitoModulo11(int soma)
    {
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
