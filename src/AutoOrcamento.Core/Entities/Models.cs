namespace AutoOrcamento.Core.Entities;

public sealed class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Nome { get; set; }
    public required string Telefone { get; set; }
    public required string Cpf { get; set; }
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
    public ICollection<Orcamento> Orcamentos { get; set; } = new List<Orcamento>();
}

public sealed class Veiculo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClienteId { get; set; }
    public required string Placa { get; set; }
    public required string Modelo { get; set; }
    public int Ano { get; set; }
    public Cliente Cliente { get; set; } = null!;
}

public enum StatusOrcamento { Pendente, Aprovado, Recusado }

public sealed class Orcamento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClienteId { get; set; }
    public Guid VeiculoId { get; set; }
    public StatusOrcamento Status { get; set; } = StatusOrcamento.Pendente;
    public decimal TotalSemDesconto { get; set; }
    public decimal TotalDesconto { get; set; }
    public decimal TotalComDesconto { get; set; }
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset AtualizadoEm { get; set; } = DateTimeOffset.UtcNow;
    public Cliente Cliente { get; set; } = null!;
    public Veiculo Veiculo { get; set; } = null!;
    public ICollection<OrcamentoItem> Itens { get; set; } = new List<OrcamentoItem>();
}

public sealed class OrcamentoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrcamentoId { get; set; }
    public required string Descricao { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal DescontoUnitario { get; set; }
    public Orcamento Orcamento { get; set; } = null!;
}
