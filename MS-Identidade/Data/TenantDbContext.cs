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
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<UsuarioFeature> UsuariosFeatures => Set<UsuarioFeature>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurarUsuario(modelBuilder);
        ConfigurarFeature(modelBuilder);
        ConfigurarUsuarioFeature(modelBuilder);
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

    private static void ConfigurarFeature(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Feature>();

        entity.ToTable("features", "identidade");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        entity.Property(x => x.IdFeaturePai)
            .HasColumnName("id_feature_pai");

        entity.Property(x => x.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true);

        entity.HasIndex(x => x.Codigo)
            .IsUnique();

        entity.HasOne<Feature>()
            .WithMany()
            .HasForeignKey(x => x.IdFeaturePai)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigurarUsuarioFeature(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UsuarioFeature>();

        entity.ToTable("usuarios_features", "identidade");

        entity.HasKey(x => new
        {
            x.UsuarioId,
            x.FeatureId
        });

        entity.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id");

        entity.Property(x => x.FeatureId)
            .HasColumnName("feature_id");

        entity.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne<Feature>()
            .WithMany()
            .HasForeignKey(x => x.FeatureId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}