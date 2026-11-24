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

    Task<List<FeatureResponseDTO>?> ListarEfetivasAsync(long empresaId, long usuarioId);

    Task<List<FeatureResponseDTO>?> ListarAtribuidasAsync(long empresaId, long usuarioId);

    Task<ResultadoAtualizacaoPermissoes> AtualizarAsync(
        long empresaId, long operadorId, long usuarioId, IReadOnlyCollection<long> featureIds);

    Task<PermissoesEmLoteResponseDTO?> ConsultarLoteAsync(long empresaId, long operadorId);

    Task<ResultadoAtualizacaoPermissoes> AtualizarEmLoteAsync(
        long empresaId, long operadorId, AtualizarPermissoesEmLoteDTO dto);
}
