using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MS_Cadastro.Entities;
using MS_Cadastro.Validations;
using MS_Cadastro.Entities.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MS_Cadastro.Data;

public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<UnidadeMedida> UnidadesMedida => Set<UnidadeMedida>();
    public DbSet<Produto> Produtos => Set<Produto>();

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
        foreach (var entrada in ChangeTracker.Entries<IDocumentoFiscal>())
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

        ConfigurarPessoa(modelBuilder.Entity<Cliente>(), "clientes", "cliente");
        ConfigurarPessoa(modelBuilder.Entity<Fornecedor>(), "fornecedores", "fornecedor");
        ConfigurarCategoria(modelBuilder.Entity<Categoria>());
        ConfigurarUnidadeMedida(modelBuilder.Entity<UnidadeMedida>());
        ConfigurarProduto(modelBuilder.Entity<Produto>());
    }

    private static void ConfigurarPessoa<T>(
        EntityTypeBuilder<T> entity,
        string tabela,
        string prefixo)
        where T : class, IPessoaCadastro
    {
        entity.ToTable(tabela, "cadastro", t =>
        {
            t.HasCheckConstraint(
                $"ck_{prefixo}_tipo_pessoa",
                "tipo_pessoa IN ('F', 'J')");

            t.HasCheckConstraint(
                $"ck_{prefixo}_nome",
                "trim(nome) <> ''");

            t.HasCheckConstraint(
                $"ck_{prefixo}_documento",
                "(tipo_pessoa = 'F' AND cpf_cnpj ~ '^[0-9]{11}$') OR " +
                "(tipo_pessoa = 'J' AND cpf_cnpj ~ '^[A-Z0-9]{12}[0-9]{2}$')");
        });

        entity.HasKey(x => x.Id).HasName($"{tabela}_pkey");

        entity.HasIndex(x => x.CpfCnpj)
            .IsUnique()
            .HasDatabaseName($"uq_{prefixo}_cpf_cnpj");

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
    private static void ConfigurarCategoria(EntityTypeBuilder<Categoria> entity)
    {
        entity.ToTable("categorias", "cadastro");

        entity.HasKey(x => x.Id);

        entity.HasIndex(x => x.Nome).IsUnique();

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        entity.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true);

        entity.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .HasDefaultValueSql("now()");
    }
    private static void ConfigurarUnidadeMedida(
        EntityTypeBuilder<UnidadeMedida> entity)
    {
        entity.ToTable("unidades_medida", "cadastro");

        entity.HasKey(x => x.Id);

        entity.HasIndex(x => x.Sigla).IsUnique();

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        entity.Property(x => x.Sigla)
            .HasColumnName("sigla")
            .HasMaxLength(10)
            .IsRequired();

        entity.Property(x => x.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true);
    }
    private static void ConfigurarProduto(EntityTypeBuilder<Produto> entity)
    {
        entity.ToTable("produtos", "cadastro");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        entity.Property(x => x.CategoriaId)
            .HasColumnName("categoria_id");

        entity.Property(x => x.UnidadeMedidaId)
            .HasColumnName("unidade_medida_id");

        entity.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(500);

        entity.Property(x => x.Tipo)
            .HasColumnName("tipo")
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(x => x.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true);

        entity.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .HasDefaultValueSql("now()");

        entity.Property(x => x.AtualizadoEm)
            .HasColumnName("atualizado_em");

        entity.HasOne(x => x.Categoria)
            .WithMany()
            .HasForeignKey(x => x.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_produto_categoria");

        entity.HasOne(x => x.UnidadeMedida)
            .WithMany()
            .HasForeignKey(x => x.UnidadeMedidaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_produto_unidade");
    }
}
