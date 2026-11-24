namespace MS_Identidade.Services.Interfaces;

public interface IPermissaoService
{
    Task<bool> PossuiPermissaoAsync(
        long empresaId,
        long usuarioId,
        string codigoFeature
    );
}