namespace MS_Cadastro.Entities;

public class UnidadeMedida
{
    public long Id { get; set; }
    public string Sigla { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}