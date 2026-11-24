using MS_Identidade.DTOs.Usuarios;

namespace MS_Identidade.Services.Interfaces;

public interface IPermissaoService
{
    Task<bool> PossuiPermissaoAsync(
        long empresaId,
        long usuarioId,
        string codigoFeature
    );

    Task<List<FeatureResponseDTO>> ListarFeaturesAsync(long empresaId);

    Task<List<FeatureResponseDTO>?> ListarAtribuidasAsync(long empresaId, long usuarioId);

    Task<ResultadoAtualizacaoPermissoes> AtualizarAsync(
        long empresaId, long operadorId, long usuarioId, IReadOnlyCollection<long> featureIds);
}
