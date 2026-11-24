using Microsoft.AspNetCore.Identity;
using MS_Identidade.Data.Interfaces;
using MS_Identidade.DTOs.Usuarios;
using MS_Identidade.Entities;
using MS_Identidade.Repositories;
using MS_Identidade.Repositories.Interfaces;
using MS_Identidade.Services.Interfaces;

namespace MS_Identidade.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly ITenantContextFactory _contextFactory;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuarioService(
        IEmpresaRepository empresaRepository,
        ITenantContextFactory contextFactory,
        IPasswordHasher<Usuario> passwordHasher)
    {
        _empresaRepository = empresaRepository;
        _contextFactory = contextFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task<UsuarioResponseDTO> CadastrarAsync(
        long empresaId,
        CadastrarUsuarioDTO dto)
    {
        var empresa = await _empresaRepository
            .ObterPorIdAsync(empresaId);

        if (empresa is null || !empresa.Ativo)
        {
            throw new InvalidOperationException(
                "Empresa inexistente ou inativa.");
        }

        await using var context = _contextFactory
            .Criar(empresa.NomeBanco);

        var repository = new UsuarioRepository(context);

        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };

        usuario.SenhaHash = _passwordHasher
            .HashPassword(usuario, dto.Senha);

        await repository.CadastrarAsync(usuario);

        return new UsuarioResponseDTO
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Ativo = usuario.Ativo,
            CriadoEm = usuario.CriadoEm
        };
    }
}