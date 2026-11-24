namespace MS_Identidade.DTOs.Auth;

public class LoginResponseDTO
{
    public long UsuarioId { get; set; }

    public long EmpresaId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }
}