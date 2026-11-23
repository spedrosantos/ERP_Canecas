using Microsoft.EntityFrameworkCore;
using MS_Identidade.Entities;

namespace MS_Identidade.Data;

public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Empresa> Empresas => Set<Empresa>();

    public DbSet<UsuarioEmpresa> UsuariosEmpresas => Set<UsuarioEmpresa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurarUsuario(modelBuilder);
        ConfigurarEmpresa(modelBuilder);
        ConfigurarUsuarioEmpresa(modelBuilder);
    }

    private static void ConfigurarUsuario(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Usuario>();

        entity.ToTable("usuarios");

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

    private static void ConfigurarUsuarioEmpresa(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UsuarioEmpresa>();

        entity.ToTable("usuarios_empresas");

        entity.HasKey(x => new
        {
            x.UsuarioId,
            x.EmpresaId
        });

        entity.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id");

        entity.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id");

        entity.Property(x => x.Perfil)
            .HasColumnName("perfil")
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true);

        entity.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .HasDefaultValueSql("NOW()");

        entity.HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Empresa)
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}