using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MS_Cadastro.Entities;
using MS_Cadastro.Validations;

namespace MS_Cadastro.Data;

public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidarDocumentos();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidarDocumentos();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidarDocumentos()
    {
        foreach (var entrada in ChangeTracker.Entries<Cliente>())
        {
            if (entrada.State != EntityState.Added
                && !(entrada.State == EntityState.Modified
                    && (entrada.Property(x => x.CpfCnpj).IsModified || entrada.Property(x => x.TipoPessoa).IsModified)))
                continue;

            var cliente = entrada.Entity;
            var normalizado = string.Empty;
            var valido = cliente.TipoPessoa switch
            {
                "F" => DocumentoFiscal.TentarNormalizarCpf(cliente.CpfCnpj, out normalizado),
                "J" => DocumentoFiscal.TentarNormalizarCnpj(cliente.CpfCnpj, out normalizado),
                _ => false
            };
            if (!valido)
                throw new ValidationException("Informe um CPF ou CNPJ válido para o tipo de pessoa.");

            entrada.Property(x => x.CpfCnpj).CurrentValue = normalizado;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entity = modelBuilder.Entity<Cliente>();
        entity.ToTable("clientes", "cadastro", tabela =>
        {
            tabela.HasCheckConstraint("ck_cliente_tipo_pessoa", "tipo_pessoa IN ('F', 'J')");
            tabela.HasCheckConstraint("ck_cliente_nome", "trim(nome) <> ''");
            tabela.HasCheckConstraint("ck_cliente_documento",
                "(tipo_pessoa = 'F' AND cpf_cnpj ~ '^[0-9]{11}$') OR " +
                "(tipo_pessoa = 'J' AND cpf_cnpj ~ '^[A-Z0-9]{12}[0-9]{2}$')");
        });

        entity.HasKey(x => x.Id).HasName("clientes_pkey");
        entity.HasIndex(x => x.CpfCnpj).IsUnique().HasDatabaseName("uq_cliente_cpf_cnpj");

        entity.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        entity.Property(x => x.TipoPessoa).HasColumnName("tipo_pessoa")
            .HasMaxLength(1).IsFixedLength().IsRequired();
        entity.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
        entity.Property(x => x.NomeFantasia).HasColumnName("nome_fantasia").HasMaxLength(150);
        entity.Property(x => x.CpfCnpj).HasColumnName("cpf_cnpj").HasMaxLength(14).IsRequired();
        entity.Property(x => x.Telefone).HasColumnName("telefone").HasMaxLength(20);
        entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(150);
        entity.Property(x => x.Cep).HasColumnName("cep").HasMaxLength(8);
        entity.Property(x => x.Logradouro).HasColumnName("logradouro").HasMaxLength(200);
        entity.Property(x => x.Numero).HasColumnName("numero").HasMaxLength(20);
        entity.Property(x => x.Complemento).HasColumnName("complemento").HasMaxLength(100);
        entity.Property(x => x.Bairro).HasColumnName("bairro").HasMaxLength(100);
        entity.Property(x => x.Cidade).HasColumnName("cidade").HasMaxLength(100);
        entity.Property(x => x.Uf).HasColumnName("uf").HasMaxLength(2).IsFixedLength();
        entity.Property(x => x.Ativo).HasColumnName("ativo").HasDefaultValue(true);
        entity.Property(x => x.CriadoEm).HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
        entity.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em")
            .HasColumnType("timestamp with time zone");
    }
}
