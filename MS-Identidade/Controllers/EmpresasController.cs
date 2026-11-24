using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Identidade.DTOs.Empresas;
using MS_Identidade.Services.Interfaces;

namespace MS_Identidade.Controllers;

[ApiController]
[Route("api/empresas")]
public class EmpresasController : ControllerBase
{
    private readonly IEmpresaTemaService _empresaTemaService;

    public EmpresasController(IEmpresaTemaService empresaTemaService)
    {
        _empresaTemaService = empresaTemaService;
    }

    [AllowAnonymous]
    [HttpGet("{empresaId:long}/tema")]
    public async Task<ActionResult<EmpresaTemaResponseDTO>> ObterTema(long empresaId)
    {
        var tema = await _empresaTemaService.ObterPorEmpresaIdAsync(empresaId);

        if (tema is null)
        {
            return NotFound();
        }

        return Ok(tema);
    }
}
