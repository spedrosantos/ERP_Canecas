using Microsoft.EntityFrameworkCore;
using MS_Identidade.Data;
using MS_Identidade.Entities;
using MS_Identidade.Repositories.Interfaces;

namespace MS_Identidade.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly TenantDbContext _context;

    public UsuarioRepository(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario> CadastrarAsync(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);

        await _context.SaveChangesAsync();

        return usuario;
    }

    public async Task<Usuario?> ObterPorIdAsync(long id)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<Usuario?> ObterParaAtualizacaoAsync(long id)
    {
        return _context.Usuarios.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task SalvarAlteracoesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<List<Usuario>> ListarAsync()
    {
        return await _context.Usuarios
            .AsNoTracking()
            .OrderBy(x => x.Nome)
            .ToListAsync();
    }
}
