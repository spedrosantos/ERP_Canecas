namespace MS_Identidade.DTOs.Usuarios;

public record PermissoesEmLoteResponseDTO(
    List<UsuarioPermissoesResponseDTO> Usuarios,
    List<FeatureResponseDTO> Features,
    HashSet<long> FeatureIdsGerenciaveis);

public record UsuarioPermissoesResponseDTO(
    long Id, string Nome, bool Ativo, bool ProprioUsuario, long[] FeatureIds);
