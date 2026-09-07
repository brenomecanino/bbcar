using AutoOrcamento.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoOrcamento.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<OrcamentoItem> OrcamentoItens => Set<OrcamentoItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Cliente>(e =>
        {
            e.ToTable("clientes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
            e.Property(x => x.Telefone).HasColumnName("telefone").HasMaxLength(20).IsRequired();
            e.Property(x => x.Cpf).HasColumnName("cpf").HasMaxLength(14).IsRequired();
            e.Property(x => x.CriadoEm).HasColumnName("criado_em");
            e.HasIndex(x => x.Cpf).HasDatabaseName("ux_clientes_cpf").IsUnique();
        });

        b.Entity<Veiculo>(e =>
        {
            e.ToTable("veiculos", t => t.HasCheckConstraint("ck_veiculos_ano", "ano >= 1950"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ClienteId).HasColumnName("cliente_id");
            e.Property(x => x.Placa).HasColumnName("placa").HasMaxLength(8).IsRequired();
            e.Property(x => x.Modelo).HasColumnName("modelo").HasMaxLength(80).IsRequired();
            e.Property(x => x.Ano).HasColumnName("ano");
            e.HasIndex(x => x.Placa).HasDatabaseName("ux_veiculos_placa").IsUnique();
            e.HasIndex(x => x.ClienteId).HasDatabaseName("ix_veiculos_cliente_id");
            e.HasOne(x => x.Cliente).WithMany(x => x.Veiculos).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Orcamento>(e =>
        {
            e.ToTable("orcamentos", t => t.HasCheckConstraint("ck_orcamentos_status", "status IN ('pendente','aprovado','recusado')"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ClienteId).HasColumnName("cliente_id");
            e.Property(x => x.VeiculoId).HasColumnName("veiculo_id");
            e.Property(x => x.Status).HasColumnName("status").HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<StatusOrcamento>(v, true)).HasMaxLength(12);
            e.Property(x => x.TotalSemDesconto).HasColumnName("total_sem_desconto").HasPrecision(12, 2);
            e.Property(x => x.TotalDesconto).HasColumnName("total_desconto").HasPrecision(12, 2);
            e.Property(x => x.TotalComDesconto).HasColumnName("total_com_desconto").HasPrecision(12, 2);
            e.Property(x => x.CriadoEm).HasColumnName("criado_em");
            e.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em");
            e.HasIndex(x => x.CriadoEm).HasDatabaseName("ix_orcamentos_criado_em").IsDescending();
            e.HasIndex(x => x.ClienteId).HasDatabaseName("ix_orcamentos_cliente_id");
            e.HasIndex(x => x.VeiculoId).HasDatabaseName("ix_orcamentos_veiculo_id");
            e.HasOne(x => x.Cliente).WithMany(x => x.Orcamentos).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Veiculo).WithMany().HasForeignKey(x => x.VeiculoId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<OrcamentoItem>(e =>
        {
            e.ToTable("orcamento_itens", t =>
            {
                t.HasCheckConstraint("ck_itens_quantidade", "quantidade > 0");
                t.HasCheckConstraint("ck_itens_valores", "valor_unitario >= 0 AND desconto_unitario >= 0 AND desconto_unitario <= valor_unitario");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrcamentoId).HasColumnName("orcamento_id");
            e.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(200).IsRequired();
            e.Property(x => x.Quantidade).HasColumnName("quantidade").HasPrecision(10, 2);
            e.Property(x => x.ValorUnitario).HasColumnName("valor_unitario").HasPrecision(12, 2);
            e.Property(x => x.DescontoUnitario).HasColumnName("desconto_unitario").HasPrecision(12, 2);
            e.HasIndex(x => x.OrcamentoId).HasDatabaseName("ix_itens_orcamento_id");
            e.HasOne(x => x.Orcamento).WithMany(x => x.Itens).HasForeignKey(x => x.OrcamentoId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
