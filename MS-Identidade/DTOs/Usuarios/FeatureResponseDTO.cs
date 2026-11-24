namespace MS_Identidade.DTOs.Usuarios;

public record FeatureResponseDTO(
    long Id,
    long? IdFeaturePai,
    string Codigo,
    string Nome,
    bool Ativo);
