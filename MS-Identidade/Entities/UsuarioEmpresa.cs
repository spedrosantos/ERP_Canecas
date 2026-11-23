namespace MS_Identidade.Entities;

public class UsuarioEmpresa
{
    public long UsuarioId { get; set; }

    public long EmpresaId { get; set; }

    public bool Ativo { get; set; } = true;

    public string Perfil { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Usuario Usuario { get; set; } = null!;

    public Empresa Empresa { get; set; } = null!;
}