using Financeiro;
using Microsoft.EntityFrameworkCore;
using MS_Compra.Entities;

namespace MS_Compra.Data;

public class TenantDbContext(DbContextOptions<TenantDbContext> options)
    : MS_Estoque.Data.TenantDbContext(options)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> Itens => Set<PedidoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigurarFinanceiro();

        var pedido = modelBuilder.Entity<Pedido>();
        pedido.ToTable("pedidos", "compras");
        pedido.HasKey(x => x.Id);
        pedido.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        pedido.Property(x => x.FornecedorId).HasColumnName("fornecedor_id");
        pedido.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        pedido.Property(x => x.Situacao).HasColumnName("situacao").HasMaxLength(20);
        pedido.Property(x => x.ValorTotal).HasColumnName("valor_total").HasPrecision(19, 2);
        pedido.Property(x => x.Observacao).HasColumnName("observacao").HasMaxLength(500);
        pedido.Property(x => x.CriadoEm).HasColumnName("criado_em");
        pedido.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
        pedido.Property(x => x.RecebidoEm).HasColumnName("recebido_em");

        pedido.HasMany(x => x.Itens).WithOne()
            .HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Restrict);

        var item = modelBuilder.Entity<PedidoItem>();
        item.ToTable("pedidos_itens", "compras");
        item.HasKey(x => x.Id);
        item.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        item.Property(x => x.PedidoId).HasColumnName("pedido_id");
        item.Property(x => x.ProdutoId).HasColumnName("produto_id");
        item.Property(x => x.Quantidade).HasColumnName("quantidade").HasPrecision(15, 3);
        item.Property(x => x.CustoUnitario).HasColumnName("custo_unitario").HasPrecision(15, 4);
    }
}
