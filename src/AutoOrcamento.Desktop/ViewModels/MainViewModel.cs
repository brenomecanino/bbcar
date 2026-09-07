using System.Collections.ObjectModel;
using System.Windows;
using AutoOrcamento.Core;
using AutoOrcamento.Core.Entities;
using AutoOrcamento.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoOrcamento.Desktop.ViewModels;

public partial class MainViewModel(IClienteService clientes, IOrcamentoService orcamentos, IPdfService pdf) : ObservableObject
{
    public ObservableCollection<OrcamentoResumoDto> Orcamentos { get; } = [];
    public ObservableCollection<ClienteComVeiculo> Clientes { get; } = [];
    public ObservableCollection<OrcamentoItemDto> Itens { get; } = [];
    [ObservableProperty] private string filtro = "";
    [ObservableProperty] private string placaBusca = "";
    [ObservableProperty] private ClienteComVeiculo? clienteSelecionado;
    [ObservableProperty] private Guid? orcamentoId;
    [ObservableProperty] private StatusOrcamento status;
    [ObservableProperty] private string descricao = "";
    [ObservableProperty] private decimal quantidade = 1;
    [ObservableProperty] private decimal valorUnitario;
    [ObservableProperty] private decimal descontoUnitario;
    [ObservableProperty] private TotaisOrcamento totais;
    [ObservableProperty] private int abaSelecionada;

    [RelayCommand] public async Task CarregarAsync() { Orcamentos.Clear(); foreach(var x in (await orcamentos.ListarOrcamentosAsync(1,Filtro)).Items) Orcamentos.Add(x); }
    [RelayCommand] public void Novo() { OrcamentoId=null;ClienteSelecionado=null;Itens.Clear();Status=StatusOrcamento.Pendente;PlacaBusca="";Recalcular();AbaSelecionada=1; }
    [RelayCommand] public async Task BuscarClienteAsync() { ClienteSelecionado=await clientes.BuscarPorPlacaAsync(PlacaBusca); if(ClienteSelecionado is null){MessageBox.Show("Cliente não localizado");AbaSelecionada=2;} }
    [RelayCommand] public void AdicionarItem() { try { var i=new OrcamentoItemDto(null,Descricao.Trim(),Quantidade,ValorUnitario,DescontoUnitario); CalculadoraOrcamento.Validar(new OrcamentoItem{Descricao=i.Descricao,Quantidade=i.Quantidade,ValorUnitario=i.ValorUnitario,DescontoUnitario=i.DescontoUnitario});Itens.Add(i);Descricao="";Quantidade=1;ValorUnitario=DescontoUnitario=0;Recalcular(); } catch(Exception e){MessageBox.Show(e.Message);} }
    [RelayCommand] public void RemoverItem(OrcamentoItemDto i) { Itens.Remove(i);Recalcular(); }
    [RelayCommand] public async Task SalvarAsync() { try { if(ClienteSelecionado is null)throw new ArgumentException("Selecione um cliente para salvar o orçamento.");var salvo=await orcamentos.SalvarOrcamentoAsync(new(OrcamentoId,ClienteSelecionado.ClienteId,ClienteSelecionado.VeiculoId,Status,Itens.ToList()));OrcamentoId=salvo.Id;await CarregarAsync();AbaSelecionada=0;}catch(Exception e){MessageBox.Show(e.Message);} }
    [RelayCommand] public async Task EditarAsync(OrcamentoResumoDto resumo) { var o=await orcamentos.ObterOrcamentoAsync(resumo.Id);OrcamentoId=o.Id;ClienteSelecionado=o.Cliente;Status=o.Status;Itens.Clear();foreach(var i in o.Itens)Itens.Add(i);Recalcular();AbaSelecionada=1; }
    [RelayCommand] public async Task ImprimirAsync(OrcamentoResumoDto? resumo) { try { if(resumo is not null)await pdf.ImprimirAsync(resumo.Id); else { await SalvarAsync();if(OrcamentoId.HasValue)await pdf.ImprimirAsync(OrcamentoId.Value); } }catch(Exception e){MessageBox.Show(e.Message);} }
    [RelayCommand] public async Task CadastrarClienteAsync(ClienteDto dto) { try { ClienteSelecionado=await clientes.CadastrarClienteAsync(dto);PlacaBusca=ClienteSelecionado.Placa;AbaSelecionada=1;}catch(Exception e){MessageBox.Show(e.Message);} }
    private void Recalcular()=>Totais=CalculadoraOrcamento.Calcular(Itens.Select(i=>new OrcamentoItem{Descricao=i.Descricao,Quantidade=i.Quantidade,ValorUnitario=i.ValorUnitario,DescontoUnitario=i.DescontoUnitario}));
}
