using MS_Identidade.DTOs.Empresas;
using MS_Identidade.Repositories.Interfaces;
using MS_Identidade.Services.Interfaces;

namespace MS_Identidade.Services;

public class EmpresaTemaService : IEmpresaTemaService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IEmpresaTemaRepository _empresaTemaRepository;

    public EmpresaTemaService(
        IEmpresaRepository empresaRepository,
        IEmpresaTemaRepository empresaTemaRepository)
    {
        _empresaRepository = empresaRepository;
        _empresaTemaRepository = empresaTemaRepository;
    }

    public async Task<EmpresaTemaResponseDTO?> ObterPorEmpresaIdAsync(long empresaId)
    {
        var empresa = await _empresaRepository.ObterPorIdAsync(empresaId);

        if (empresa is null || !empresa.Ativo)
        {
            return null;
        }

        var tema = await _empresaTemaRepository.ObterPorEmpresaIdAsync(empresaId);

        return new EmpresaTemaResponseDTO
        {
            NomeFantasia = empresa.NomeFantasia,
            CorPrimaria = tema?.CorPrimaria ?? "#1F2937",
            CorSecundaria = tema?.CorSecundaria ?? "#4B5563",
            CorFundo = tema?.CorFundo ?? "#F9FAFB",
            FonteTitulo = tema?.FonteTitulo ?? "Inter",
            FonteTexto = tema?.FonteTexto ?? "Inter",
            LogoUrl = tema?.LogoUrl
        };
    }
}
