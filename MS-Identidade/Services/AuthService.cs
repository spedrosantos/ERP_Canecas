using Microsoft.AspNetCore.Identity;
using MS_Identidade.Data.Interfaces;
using MS_Identidade.DTOs.Auth;
using MS_Identidade.Entities;
using MS_Identidade.Repositories;
using MS_Identidade.Repositories.Interfaces;
using MS_Identidade.Services.Interfaces;

namespace MS_Identidade.Services;

public class AuthService : IAuthService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly ITenantContextFactory _contextFactory;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public AuthService(
        IEmpresaRepository empresaRepository,
        ITenantContextFactory contextFactory,
        IPasswordHasher<Usuario> passwordHasher)
    {
        _empresaRepository = empresaRepository;
        _contextFactory = contextFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task<Usuario?> ValidarCredenciaisAsync(LoginDTO dto)
    {
        var empresa = await _empresaRepository
            .ObterPorIdAsync(dto.EmpresaId);

        if (empresa is null || !empresa.Ativo)
            return null;

        await using var context = _contextFactory
            .Criar(empresa.NomeBanco);

        var repository = new UsuarioRepository(context);

        var usuario = await repository
            .ObterPorIdAsync(dto.UsuarioId);

        if (usuario is null || !usuario.Ativo)
            return null;

        var resultado = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.SenhaHash,
            dto.Senha);

        if (resultado == PasswordVerificationResult.Failed)
            return null;

        return usuario;
    }
}