using Microsoft.EntityFrameworkCore;

namespace Financeiro;

public static class ModeloFinanceiro
{
    public static void ConfigurarFinanceiro(this ModelBuilder modelo)
    {
        var titulo = modelo.Entity<Titulo>();
        titulo.ToTable("titulos", "financeiro");
        titulo.HasKey(x => x.Id);
        titulo.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        titulo.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(10);
        titulo.Property(x => x.CompraId).HasColumnName("compra_id");
        titulo.Property(x => x.VendaId).HasColumnName("venda_id");
        titulo.HasIndex(x => x.CompraId).IsUnique();
        titulo.HasIndex(x => x.VendaId).IsUnique();
        titulo.Property(x => x.Valor).HasColumnName("valor").HasPrecision(19, 2);
        titulo.Property(x => x.Vencimento).HasColumnName("vencimento");
        titulo.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        titulo.Property(x => x.CriadoEm).HasColumnName("criado_em");
        titulo.Property(x => x.CanceladoEm).HasColumnName("cancelado_em");
        titulo.HasMany(x => x.Baixas).WithOne().HasForeignKey(x => x.TituloId)
            .OnDelete(DeleteBehavior.Restrict);

        var baixa = modelo.Entity<Baixa>();
        baixa.ToTable("baixas", "financeiro");
        baixa.HasKey(x => x.Id);
        baixa.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        baixa.Property(x => x.TituloId).HasColumnName("titulo_id");
        baixa.Property(x => x.Valor).HasColumnName("valor").HasPrecision(19, 2);
        baixa.Property(x => x.FormaPagamento).HasColumnName("forma_pagamento").HasMaxLength(20);
        baixa.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        baixa.Property(x => x.Observacao).HasColumnName("observacao").HasMaxLength(500);
        baixa.Property(x => x.CriadoEm).HasColumnName("criado_em");
        baixa.Property(x => x.EstornadoEm).HasColumnName("estornado_em");
        baixa.Property(x => x.UsuarioEstornoId).HasColumnName("usuario_estorno_id");
        baixa.Property(x => x.MotivoEstorno).HasColumnName("motivo_estorno").HasMaxLength(500);
    }
}
