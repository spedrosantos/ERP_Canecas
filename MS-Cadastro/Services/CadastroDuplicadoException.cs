namespace MS_Cadastro.Services;

public class CadastroDuplicadoException : Exception
{
    public CadastroDuplicadoException(string mensagem)
        : base(mensagem)
    {
    }
}