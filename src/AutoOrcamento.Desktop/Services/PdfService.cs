using System.Diagnostics;
using System.IO;
using AutoOrcamento.Core.Services;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoOrcamento.Desktop.Services;

public sealed class PdfService(IOrcamentoService orcamentos) : IPdfService
{
    public async Task<string> GerarPdfAsync(Guid id, string? caminhoSaida)
    {
        var o=await orcamentos.ObterOrcamentoAsync(id); var path=caminhoSaida ?? Path.Combine(Path.GetTempPath(),$"orcamento_{o.Cliente.Placa}_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        Document.Create(d=>d.Page(p=>{p.Size(PageSizes.A4);p.Margin(35);p.DefaultTextStyle(x=>x.FontSize(10));p.Header().Row(r=>{r.RelativeItem().Text("AUTO ORÇAMENTO").Bold().FontSize(18);r.ConstantItem(180).AlignRight().Text($"ORÇAMENTO #{o.Id.ToString()[..8].ToUpper()}\n{o.CriadoEm:dd/MM/yyyy}");});p.Content().PaddingVertical(20).Column(c=>{c.Spacing(12);c.Item().Text($"Cliente: {o.Cliente.Nome}   Telefone: {o.Cliente.Telefone}   CPF: {o.Cliente.Cpf}");c.Item().Text($"Veículo: {o.Cliente.Modelo}/{o.Cliente.Ano}   Placa: {o.Cliente.Placa}");c.Item().Table(t=>{t.ColumnsDefinition(x=>{x.RelativeColumn(4);x.RelativeColumn();x.RelativeColumn();x.RelativeColumn();x.RelativeColumn();});t.Header(h=>{foreach(var s in new[]{"Descrição","Qtd","Vlr Unit","Desc","Subtotal"})h.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(s).Bold();});foreach(var i in o.Itens){t.Cell().Padding(3).Text(i.Descricao);t.Cell().Padding(3).Text(i.Quantidade.ToString("N2"));t.Cell().Padding(3).Text(i.ValorUnitario.ToString("C"));t.Cell().Padding(3).Text(i.DescontoUnitario.ToString("C"));t.Cell().Padding(3).Text((i.Quantidade*(i.ValorUnitario-i.DescontoUnitario)).ToString("C"));}});c.Item().AlignRight().Text($"Total sem desconto: {o.Totais.TotalSemDesconto:C}\nDesconto: {o.Totais.TotalDesconto:C} ({o.Totais.PercentualDesconto:N2}%)\nTOTAL: {o.Totais.TotalComDesconto:C}").Bold();});p.Footer().Column(c=>{c.Item().Text($"Status: {o.Status.ToString().ToLowerInvariant()} | Validade: 15 dias");c.Item().PaddingTop(25).AlignCenter().Text("__________________________________\nAssinatura do cliente");});})).GeneratePdf(path); return path;
    }
    public async Task ImprimirAsync(Guid id) { var path=await GerarPdfAsync(id,null); Process.Start(new ProcessStartInfo(path){UseShellExecute=true,Verb="print"}); }
    public async Task<string?> SalvarComoAsync(Guid id) { var dialog=new SaveFileDialog{Filter="PDF (*.pdf)|*.pdf",FileName=$"orcamento_{id.ToString()[..8]}.pdf"}; return dialog.ShowDialog()==true?await GerarPdfAsync(id,dialog.FileName):null; }
}
