using Financeiro;
using Microsoft.EntityFrameworkCore;

namespace MS_Financeiro.Data;

public class TenantDbContext(DbContextOptions<TenantDbContext> options) : DbContext(options)
{
    public DbSet<Baixa> Baixas => Set<Baixa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ConfigurarFinanceiro();
}
