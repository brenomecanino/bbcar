using AutoOrcamento.Core.Entities;

namespace AutoOrcamento.Core;

public readonly record struct TotaisOrcamento(decimal TotalSemDesconto, decimal TotalDesconto, decimal TotalComDesconto, decimal PercentualDesconto);

public static class CalculadoraOrcamento
{
    public static TotaisOrcamento Calcular(IEnumerable<OrcamentoItem> itens)
    {
        ArgumentNullException.ThrowIfNull(itens);
        decimal total = 0, desconto = 0;
        foreach (var item in itens)
        {
            Validar(item);
            total += item.Quantidade * item.ValorUnitario;
            desconto += item.Quantidade * item.DescontoUnitario;
        }
        total = Arredondar(total);
        desconto = Arredondar(desconto);
        var comDesconto = Arredondar(total - desconto);
        var percentual = total == 0 ? 0 : Arredondar(desconto / total * 100);
        return new(total, desconto, comDesconto, percentual);
    }

    public static void Validar(OrcamentoItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Descricao) || item.Descricao.Length > 200) throw new ArgumentException("A descrição deve ter entre 1 e 200 caracteres.");
        if (item.Quantidade <= 0) throw new ArgumentException("A quantidade deve ser maior que zero.");
        if (item.ValorUnitario < 0) throw new ArgumentException("O valor unitário não pode ser negativo.");
        if (item.DescontoUnitario < 0 || item.DescontoUnitario > item.ValorUnitario) throw new ArgumentException("O desconto deve estar entre zero e o valor unitário.");
    }

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
