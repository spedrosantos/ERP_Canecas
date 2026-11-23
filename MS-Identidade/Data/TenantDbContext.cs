using Microsoft.EntityFrameworkCore;
using MS_Identidade.Entities;

namespace MS_Identidade.Data;

public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurarUsuario(modelBuilder);
    }

    private static void ConfigurarUsuario(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Usuario>();

        entity.ToTable("usuarios", "identidade");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        entity.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.SenhaHash)
            .HasColumnName("senha_hash")
            .HasMaxLength(255)
            .IsRequired();

        entity.Property(x => x.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true);

        entity.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .HasDefaultValueSql("NOW()");
    }
}