namespace MS_Cadastro.Services;

public sealed class ClienteDocumentoDuplicadoException(Exception causa)
    : Exception("Já existe um cliente com este CPF ou CNPJ na empresa.", causa);
