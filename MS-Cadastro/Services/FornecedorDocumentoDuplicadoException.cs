namespace MS_Cadastro.Services;

public sealed class FornecedorDocumentoDuplicadoException(Exception causa)
    : Exception("Já existe um fornecedor com este CPF ou CNPJ na empresa.", causa);
