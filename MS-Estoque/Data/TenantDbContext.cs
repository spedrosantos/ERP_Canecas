using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MS_Estoque.Entities;

namespace MS_Estoque.Data;

public class TenantDbContext(
    DbContextOptions<TenantDbContext> options)
    : DbContext(options)
{
    public DbSet<Saldo> Saldos => Set<Saldo>();

    public DbSet<MovimentacaoEstoque> Movimentacoes =>
        Set<MovimentacaoEstoque>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurarSaldo(modelBuilder.Entity<Saldo>());
        ConfigurarMovimentacao(
            modelBuilder.Entity<MovimentacaoEstoque>());
    }

    private static void ConfigurarSaldo(
        EntityTypeBuilder<Saldo> entity)
    {
        entity.ToTable("saldos", "estoque");

        entity.HasKey(x => x.ProdutoId);

        entity.Property(x => x.ProdutoId)
            .HasColumnName("produto_id")
            .ValueGeneratedNever();

        entity.Property(x => x.Quantidade)
            .HasColumnName("quantidade")
            .HasPrecision(15, 3);

        entity.Property(x => x.EstoqueMinimo)
            .HasColumnName("estoque_minimo")
            .HasPrecision(15, 3);

        entity.Property(x => x.CustoMedio)
            .HasColumnName("custo_medio")
            .HasPrecision(15, 4);

        entity.Property(x => x.AtualizadoEm)
            .HasColumnName("atualizado_em")
            .HasDefaultValueSql("now()");
    }

    private static void ConfigurarMovimentacao(
        EntityTypeBuilder<MovimentacaoEstoque> entity)
    {
        entity.ToTable("movimentacoes", "estoque");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        entity.Property(x => x.ProdutoId)
            .HasColumnName("produto_id");

        entity.Property(x => x.Tipo)
            .HasColumnName("tipo")
            .HasMaxLength(10)
            .IsRequired();

        entity.Property(x => x.Quantidade)
            .HasColumnName("quantidade")
            .HasPrecision(15, 3);

        entity.Property(x => x.CustoUnitario)
            .HasColumnName("custo_unitario")
            .HasPrecision(15, 4);

        entity.Property(x => x.Origem)
            .HasColumnName("origem")
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(x => x.ReferenciaId)
            .HasColumnName("referencia_id");

        entity.Property(x => x.MovimentacaoOriginalId)
            .HasColumnName("movimentacao_original_id");

        entity.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id");

        entity.Property(x => x.Justificativa)
            .HasColumnName("justificativa")
            .HasMaxLength(500);

        entity.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .HasDefaultValueSql("now()");

        entity.HasIndex(x => x.MovimentacaoOriginalId)
            .IsUnique();

        entity.HasOne<MovimentacaoEstoque>()
            .WithMany()
            .HasForeignKey(x => x.MovimentacaoOriginalId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_movimentacao_original");
    }
}
