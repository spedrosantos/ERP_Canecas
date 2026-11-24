namespace MS_Identidade.DTOs.Empresas;

public class EmpresaTemaResponseDTO
{
    public string NomeFantasia { get; set; } = string.Empty;

    public string CorPrimaria { get; set; } = string.Empty;

    public string CorSecundaria { get; set; } = string.Empty;

    public string CorFundo { get; set; } = string.Empty;

    public string FonteTitulo { get; set; } = string.Empty;

    public string FonteTexto { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }
}
