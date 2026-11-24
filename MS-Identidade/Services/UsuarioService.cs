using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MS_Identidade.Data;
using MS_Identidade.Data.Interfaces;
using MS_Identidade.DTOs.Usuarios;
using MS_Identidade.Entities;
using MS_Identidade.Repositories;
using MS_Identidade.Repositories.Interfaces;
using MS_Identidade.Services.Interfaces;
using Npgsql;

namespace MS_Identidade.Services;

public class UsuarioService : IUsuarioService
{
    private static readonly string[] PermissoesGerenciamento =
        ["USUARIO_EDITAR", "USUARIO_PERMISSOES"];

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

    public async Task<List<UsuarioResponseDTO>> ListarAsync(long empresaId)
    {
        await using var context = await CriarContextoAsync(empresaId);
        var repository = new UsuarioRepository(context);

        var usuarios = await repository.ListarAsync();

        return usuarios.ConvertAll(MapearUsuario);
    }

    public async Task<UsuarioResponseDTO> CadastrarAsync(
        long empresaId,
        CadastrarUsuarioDTO dto)
    {
        await using var context = await CriarContextoAsync(empresaId);
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

        return MapearUsuario(usuario);
    }

    public async Task<UsuarioResponseDTO?> EditarAsync(
        long empresaId, long id, EditarUsuarioDTO dto)
    {
        await using var context = await CriarContextoAsync(empresaId);
        var repository = new UsuarioRepository(context);
        var usuario = await repository.ObterParaAtualizacaoAsync(id);

        if (usuario is null)
            return null;

        usuario.Nome = dto.Nome.Trim();

        if (dto.Senha is not null)
            usuario.SenhaHash = _passwordHasher.HashPassword(usuario, dto.Senha);

        await repository.SalvarAlteracoesAsync();

        return MapearUsuario(usuario);
    }

    public async Task<(UsuarioResponseDTO? Usuario, string? Conflito)> AlterarSituacaoAsync(
        long empresaId, long usuarioAutenticadoId, long id, bool ativo)
    {
        await using var context = await CriarContextoAsync(empresaId);
        var repository = new UsuarioRepository(context);

        try
        {
            // Mantém a verificação de permissões e a atualização atômicas entre requisições.
            await using var transacao = await context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var usuario = await repository.ObterParaAtualizacaoAsync(id);

            if (usuario is null)
                return (null, null);

            if (!ativo && id == usuarioAutenticadoId)
                return (null, "Você não pode desativar a própria conta.");

            if (!ativo && usuario.Ativo && await RemoveriaUltimoAdministradorAsync(context, id))
                return (null, "A desativação deixaria a empresa sem um administrador ativo para uma permissão de gerenciamento.");

            usuario.Ativo = ativo;
            await repository.SalvarAlteracoesAsync();
            await transacao.CommitAsync();

            return (MapearUsuario(usuario), null);
        }
        catch (Exception ex) when (
            ex.GetBaseException() is PostgresException
                { SqlState: PostgresErrorCodes.SerializationFailure })
        {
            return (null, "A situação dos usuários foi alterada simultaneamente. Tente novamente.");
        }
    }

    private static async Task<bool> RemoveriaUltimoAdministradorAsync(
        TenantDbContext context, long usuarioId)
    {
        var permissoes = await new PermissaoRepository(context)
            .ListarPorUsuariosAtivosAsync();

        return PermissoesGerenciamento.Any(codigo =>
            PermissaoService.PossuiPermissaoEfetiva(permissoes[usuarioId], codigo)
            && !permissoes.Any(outro => outro.Key != usuarioId
                && PermissaoService.PossuiPermissaoEfetiva(outro, codigo)));
    }

    private async Task<TenantDbContext> CriarContextoAsync(long empresaId)
    {
        var empresa = await _empresaRepository.ObterPorIdAsync(empresaId);

        if (empresa is null || !empresa.Ativo)
        {
            throw new InvalidOperationException(
                "Empresa inexistente ou inativa.");
        }

        return _contextFactory.Criar(empresa.NomeBanco);
    }

    private static UsuarioResponseDTO MapearUsuario(Usuario usuario)
    {
        return new UsuarioResponseDTO
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Ativo = usuario.Ativo,
            CriadoEm = usuario.CriadoEm
        };
    }
}
