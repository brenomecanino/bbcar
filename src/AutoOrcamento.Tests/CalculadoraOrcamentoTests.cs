using AutoOrcamento.Core;
using AutoOrcamento.Core.Entities;

namespace AutoOrcamento.Tests;

public sealed class CalculadoraOrcamentoTests
{
    [Fact] public void SemDesconto() => Assert.Equal(new(200m,0m,200m,0m), Calcular((2m,100m,0m)));
    [Fact] public void ComDesconto() => Assert.Equal(new(200m,20m,180m,10m), Calcular((2m,100m,10m)));
    [Fact] public void Misto() => Assert.Equal(new(250m,20m,230m,8m), Calcular((2m,100m,10m),(1m,50m,0m)));
    [Fact] public void DescontoTotal() => Assert.Equal(new(80m,80m,0m,100m), Calcular((1m,80m,80m)));
    [Fact] public void ArredondaLongeDeZero() { var t=Calcular((1m,1.005m,0m));Assert.Equal(1.01m,t.TotalSemDesconto); }
    [Fact] public void RejeitaDescontoMaiorQueValor() => Assert.Throws<ArgumentException>(()=>Calcular((1m,10m,11m)));
    [Fact] public void PercentualZeroQuandoTotalZero() => Assert.Equal(0m,Calcular((1m,0m,0m)).PercentualDesconto);
    private static TotaisOrcamento Calcular(params (decimal q,decimal v,decimal d)[] xs)=>CalculadoraOrcamento.Calcular(xs.Select(x=>new OrcamentoItem{Descricao="Item",Quantidade=x.q,ValorUnitario=x.v,DescontoUnitario=x.d}));
}
