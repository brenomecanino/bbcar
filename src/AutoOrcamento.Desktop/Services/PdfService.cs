using System.Diagnostics;
using System.IO;
using AutoOrcamento.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoOrcamento.Desktop.Services;

public sealed class PdfService(IOrcamentoService orcamentos, IConfiguration configuration) : IPdfService
{
    public async Task<string> GerarPdfAsync(Guid id, string? caminhoSaida)
    {
        var orcamento = await orcamentos.ObterOrcamentoAsync(id);
        var caminho = caminhoSaida ?? Path.Combine(Path.GetTempPath(), "AutoOrcamento", $"orcamento_{orcamento.Id:N}.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        var oficina = configuration["Oficina:Nome"] ?? "Auto Orçamento";
        var validade = configuration.GetValue("Oficina:ValidadeDias", 15);

        Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(35);
            pagina.DefaultTextStyle(x => x.FontSize(10));
            pagina.Header().Row(linha =>
            {
                linha.RelativeItem().Text(oficina).Bold().FontSize(18);
                linha.ConstantItem(180).AlignRight().Text($"ORÇAMENTO #{orcamento.Id.ToString()[..8].ToUpper()}\n{orcamento.CriadoEm:dd/MM/yyyy}");
            });
            pagina.Content().PaddingVertical(20).Column(coluna =>
            {
                coluna.Spacing(12);
                coluna.Item().Text($"Cliente: {orcamento.Cliente.Nome}   Telefone: {orcamento.Cliente.Telefone}   CPF: {orcamento.Cliente.Cpf}");
                coluna.Item().Text($"Veículo: {orcamento.Cliente.Modelo}/{orcamento.Cliente.Ano}   Placa: {orcamento.Cliente.Placa}");
                coluna.Item().Table(tabela =>
                {
                    tabela.ColumnsDefinition(x => { x.RelativeColumn(4); x.RelativeColumn(); x.RelativeColumn(); x.RelativeColumn(); x.RelativeColumn(); });
                    tabela.Header(cabecalho =>
                    {
                        foreach (var titulo in new[] { "Descrição", "Qtd", "Vlr Unit", "Desc", "Subtotal" })
                            cabecalho.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(titulo).Bold();
                    });
                    foreach (var item in orcamento.Itens)
                    {
                        tabela.Cell().Padding(3).Text(item.Descricao);
                        tabela.Cell().Padding(3).Text(item.Quantidade.ToString("N2"));
                        tabela.Cell().Padding(3).Text(item.ValorUnitario.ToString("C"));
                        tabela.Cell().Padding(3).Text(item.DescontoUnitario.ToString("C"));
                        tabela.Cell().Padding(3).Text((item.Quantidade * (item.ValorUnitario - item.DescontoUnitario)).ToString("C"));
                    }
                });
                coluna.Item().AlignRight().Text($"Total sem desconto: {orcamento.Totais.TotalSemDesconto:C}\nDesconto: {orcamento.Totais.TotalDesconto:C} ({orcamento.Totais.PercentualDesconto:N2}%)\nTOTAL: {orcamento.Totais.TotalComDesconto:C}").Bold();
            });
            pagina.Footer().Column(coluna =>
            {
                coluna.Item().Text($"Status: {orcamento.Status.ToString().ToLowerInvariant()} | Validade: {validade} dias");
                coluna.Item().PaddingTop(25).AlignCenter().Text("__________________________________\nAssinatura do cliente");
            });
        })).GeneratePdf(caminho);
        return caminho;
    }

    public async Task ImprimirAsync(Guid id)
    {
        var caminho = await GerarPdfAsync(id, null);
        var arquivo = Path.GetFullPath(caminho);
        if (!File.Exists(arquivo)) throw new FileNotFoundException("O PDF do orçamento não foi gerado.", arquivo);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = arquivo,
                UseShellExecute = true,
                Verb = "print"
            });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = arquivo,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }

    public async Task<string?> SalvarComoAsync(Guid id)
    {
        var dialogo = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = $"orcamento_{id.ToString()[..8]}.pdf" };
        return dialogo.ShowDialog() == true ? await GerarPdfAsync(id, dialogo.FileName) : null;
    }
}
