namespace MS_Identidade.Entities;

public class Usuario
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string SenhaHash { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}