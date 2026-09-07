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
        b.Entity<Cliente>(e => { e.ToTable("clientes"); e.HasKey(x => x.Id); e.Property(x => x.Nome).HasMaxLength(120).IsRequired(); e.Property(x => x.Telefone).HasMaxLength(20).IsRequired(); e.Property(x => x.Cpf).HasMaxLength(14).IsRequired(); e.HasIndex(x => x.Cpf).IsUnique(); });
        b.Entity<Veiculo>(e => { e.ToTable("veiculos"); e.HasKey(x => x.Id); e.Property(x => x.Placa).HasMaxLength(8).IsRequired(); e.Property(x => x.Modelo).HasMaxLength(80).IsRequired(); e.HasIndex(x => x.Placa).IsUnique(); e.HasIndex(x => x.ClienteId); e.HasCheckConstraint("ck_veiculos_ano", "ano >= 1950"); e.HasOne(x => x.Cliente).WithMany(x => x.Veiculos).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<Orcamento>(e => { e.ToTable("orcamentos"); e.HasKey(x => x.Id); e.Property(x => x.Status).HasConversion(v => v.ToString().ToLowerInvariant(), v => Enum.Parse<StatusOrcamento>(v, true)).HasMaxLength(12); e.Property(x => x.TotalSemDesconto).HasPrecision(12,2); e.Property(x => x.TotalDesconto).HasPrecision(12,2); e.Property(x => x.TotalComDesconto).HasPrecision(12,2); e.HasIndex(x => x.CriadoEm).IsDescending(); e.HasIndex(x => x.ClienteId); e.HasOne(x => x.Cliente).WithMany(x => x.Orcamentos).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Veiculo).WithMany().HasForeignKey(x => x.VeiculoId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<OrcamentoItem>(e => { e.ToTable("orcamento_itens"); e.HasKey(x => x.Id); e.Property(x => x.Descricao).HasMaxLength(200).IsRequired(); e.Property(x => x.Quantidade).HasPrecision(10,2); e.Property(x => x.ValorUnitario).HasPrecision(12,2); e.Property(x => x.DescontoUnitario).HasPrecision(12,2); e.HasIndex(x => x.OrcamentoId); e.HasCheckConstraint("ck_itens_quantidade", "quantidade > 0"); e.HasCheckConstraint("ck_itens_valores", "valor_unitario >= 0 AND desconto_unitario >= 0 AND desconto_unitario <= valor_unitario"); e.HasOne(x => x.Orcamento).WithMany(x => x.Itens).HasForeignKey(x => x.OrcamentoId).OnDelete(DeleteBehavior.Cascade); });
    }
}
