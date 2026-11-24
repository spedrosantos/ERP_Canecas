namespace MS_Identidade.DTOs.Usuarios;

public class UsuarioResponseDTO
{
    public long Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }
}