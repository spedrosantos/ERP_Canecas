using Microsoft.EntityFrameworkCore;
using MS_Identidade.Entities;

namespace MS_Identidade.Data;

public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options)
        : base(options)
    {
    }

    public DbSet<Empresa> Empresas => Set<Empresa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurarEmpresa(modelBuilder);
    }

    private static void ConfigurarEmpresa(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Empresa>();

        entity.ToTable("empresas");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        entity.Property(x => x.RazaoSocial)
            .HasColumnName("razao_social")
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.NomeFantasia)
            .HasColumnName("nome_fantasia")
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.Cnpj)
            .HasColumnName("cnpj")
            .HasMaxLength(14)
            .IsRequired();

        entity.Property(x => x.NomeBanco)
            .HasColumnName("nome_banco")
            .HasMaxLength(63)
            .IsRequired();

        entity.Property(x => x.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true);

        entity.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .HasDefaultValueSql("NOW()");

        entity.HasIndex(x => x.Cnpj)
            .IsUnique();

        entity.HasIndex(x => x.NomeBanco)
            .IsUnique();
    }
}