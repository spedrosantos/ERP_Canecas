namespace MS_Identidade.Entities;

public class Empresa
{
    public long Id { get; set; }

    public string RazaoSocial { get; set; } = string.Empty;

    public string NomeFantasia { get; set; } = string.Empty;

    public string Cnpj { get; set; } = string.Empty;

    public string NomeBanco { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}