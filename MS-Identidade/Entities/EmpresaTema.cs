namespace MS_Identidade.Entities;

public class EmpresaTema
{
    public long EmpresaId { get; set; }

    public string CorPrimaria { get; set; } = string.Empty;

    public string CorSecundaria { get; set; } = string.Empty;

    public string CorFundo { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public string FonteTitulo { get; set; } = "Inter";

    public string FonteTexto { get; set; } = "Inter";
}