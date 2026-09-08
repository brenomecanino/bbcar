using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using AutoOrcamento.Core;
using AutoOrcamento.Core.Entities;
using AutoOrcamento.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoOrcamento.Desktop.ViewModels;

public partial class ItemOrcamentoViewModel : ObservableObject
{
    public Guid? Id { get; init; }
    [ObservableProperty] private string descricao = "";
    [ObservableProperty] private decimal quantidade;
    [ObservableProperty] private decimal valorUnitario;
    [ObservableProperty] private decimal descontoUnitario;
    public decimal Subtotal => Math.Round(Quantidade * (ValorUnitario - DescontoUnitario), 2, MidpointRounding.AwayFromZero);
    partial void OnQuantidadeChanged(decimal value) => OnPropertyChanged(nameof(Subtotal));
    partial void OnValorUnitarioChanged(decimal value) => OnPropertyChanged(nameof(Subtotal));
    partial void OnDescontoUnitarioChanged(decimal value) => OnPropertyChanged(nameof(Subtotal));
    partial void OnDescricaoChanged(string value)
    {
        var normalizado = value.ToUpperInvariant();
        if (value != normalizado) Descricao = normalizado;
    }
    public OrcamentoItemDto ToDto() => new(Id, Descricao, Quantidade, ValorUnitario, DescontoUnitario);
}

public partial class MainViewModel : ObservableObject
{
    private readonly IClienteService clientes;
    private readonly IOrcamentoService orcamentos;
    private readonly IPdfService pdf;
    private bool carregando;
    public MainViewModel(IClienteService clientes, IOrcamentoService orcamentos, IPdfService pdf) { this.clientes = clientes; this.orcamentos = orcamentos; this.pdf = pdf; Itens.CollectionChanged += ItensAlterados; }
    public ObservableCollection<OrcamentoResumoDto> Orcamentos { get; } = [];
    public ObservableCollection<ClienteComVeiculo> Clientes { get; } = [];
    public ObservableCollection<ItemOrcamentoViewModel> Itens { get; } = [];
    public IReadOnlyList<StatusOrcamento> StatusDisponiveis { get; } = Enum.GetValues<StatusOrcamento>();
    [ObservableProperty] private string filtro = "";
    [ObservableProperty] private DateTime? dataInicial;
    [ObservableProperty] private DateTime? dataFinal;
    [ObservableProperty] private int paginaAtual = 1;
    [ObservableProperty] private int totalPaginas = 1;
    public bool PodePaginaAnterior => PaginaAtual > 1;
    public bool PodeProximaPagina => PaginaAtual < TotalPaginas;
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
    [ObservableProperty] private bool alterado;
    [ObservableProperty] private string mensagemValidacao = "";
    public bool ClienteBloqueado => ClienteSelecionado is not null;
    public string ClienteDescricao => ClienteSelecionado is null ? "Nenhum cliente selecionado" : $"{ClienteSelecionado.Nome} — {ClienteSelecionado.Modelo}/{ClienteSelecionado.Ano}";
    partial void OnDescricaoChanged(string value)
    {
        var normalizado = value.ToUpperInvariant();
        if (value != normalizado) Descricao = normalizado;
    }

    partial void OnClienteSelecionadoChanged(ClienteComVeiculo? value) { OnPropertyChanged(nameof(ClienteBloqueado)); OnPropertyChanged(nameof(ClienteDescricao)); MarcarAlterado(); }
    partial void OnStatusChanged(StatusOrcamento value) => MarcarAlterado();    [RelayCommand] public async Task CarregarAsync()
    {
        Orcamentos.Clear();
        var resultado = await orcamentos.ListarOrcamentosAsync(PaginaAtual, Filtro, DataInicial, DataFinal);
        foreach (var item in resultado.Items) Orcamentos.Add(item);
        TotalPaginas = Math.Max(1, (int)Math.Ceiling(resultado.Total / 50m));
        OnPropertyChanged(nameof(PodePaginaAnterior));
        OnPropertyChanged(nameof(PodeProximaPagina));
        Clientes.Clear();
        foreach (var cliente in await clientes.ListarAsync()) Clientes.Add(cliente);
    }
    [RelayCommand] public async Task PaginaAnteriorAsync() { if (!PodePaginaAnterior) return; PaginaAtual--; await CarregarAsync(); }
    [RelayCommand] public async Task ProximaPaginaAsync() { if (!PodeProximaPagina) return; PaginaAtual++; await CarregarAsync(); }
    [RelayCommand] public void Novo() { carregando = true; OrcamentoId = null; ClienteSelecionado = null; Itens.Clear(); Status = StatusOrcamento.Pendente; PlacaBusca = ""; Recalcular(); Alterado = false; carregando = false; AbaSelecionada = 1; }
    [RelayCommand] public void TrocarCliente() { ClienteSelecionado = null; PlacaBusca = ""; }
    [RelayCommand] public async Task BuscarClienteAsync() { ClienteSelecionado = await clientes.BuscarPorPlacaAsync(PlacaBusca); if (ClienteSelecionado is null) { MessageBox.Show("Cliente não localizado"); AbaSelecionada = 2; } }
    [RelayCommand] public void AdicionarItem() { try { var item = new ItemOrcamentoViewModel { Descricao = Descricao.Trim(), Quantidade = Quantidade, ValorUnitario = ValorUnitario, DescontoUnitario = DescontoUnitario }; Validar(item); Itens.Add(item); Descricao = ""; Quantidade = 1; ValorUnitario = DescontoUnitario = 0; } catch (Exception e) { MessageBox.Show(e.Message); } }
    [RelayCommand] public void RemoverItem(ItemOrcamentoViewModel item) => Itens.Remove(item);
    [RelayCommand] public async Task SalvarAsync() { try { await PersistirAsync(); await CarregarAsync(); AbaSelecionada = 0; } catch (Exception e) { MessageBox.Show(e.Message); } }
    [RelayCommand] public async Task EditarAsync(OrcamentoResumoDto resumo) { var o = await orcamentos.ObterOrcamentoAsync(resumo.Id); carregando = true; OrcamentoId = o.Id; ClienteSelecionado = o.Cliente; PlacaBusca = o.Cliente.Placa; Status = o.Status; Itens.Clear(); foreach (var item in o.Itens) Itens.Add(new ItemOrcamentoViewModel { Id = item.Id, Descricao = item.Descricao, Quantidade = item.Quantidade, ValorUnitario = item.ValorUnitario, DescontoUnitario = item.DescontoUnitario }); Recalcular(); Alterado = false; carregando = false; AbaSelecionada = 1; }
    [RelayCommand] public async Task ImprimirAsync(OrcamentoResumoDto? resumo)
    {
        try
        {
            if (resumo is null && (OrcamentoId is null || Alterado))
            {
                if (OrcamentoId is not null && MessageBox.Show("Salvar alterações antes de imprimir?", "Imprimir", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                await PersistirAsync();
            }
            else if (resumo is not null && OrcamentoId == resumo.Id && Alterado)
            {
                if (MessageBox.Show("Salvar alterações antes de imprimir?", "Imprimir", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                await PersistirAsync();
            }

            var id = resumo?.Id ?? OrcamentoId;
            if (id is null) throw new ArgumentException("Selecione um orçamento para imprimir.");
            await pdf.ImprimirAsync(id.Value);
        }
        catch (Exception e) { MessageBox.Show(e.Message); }
    }
    [RelayCommand] public async Task SalvarComoAsync() { try { if (OrcamentoId is null || Alterado) await PersistirAsync(); await pdf.SalvarComoAsync(OrcamentoId!.Value); } catch (Exception e) { MessageBox.Show(e.Message); } }
    [RelayCommand] public async Task CadastrarClienteAsync(ClienteDto dto) { try { ClienteSelecionado = await clientes.CadastrarClienteAsync(dto); PlacaBusca = ClienteSelecionado.Placa; await CarregarAsync(); AbaSelecionada = 1; } catch (Exception e) { MessageBox.Show(e.Message); } }
    [RelayCommand] public async Task AtualizarClienteAsync(ClienteComVeiculo cliente) { try { await clientes.AtualizarAsync(cliente); await CarregarAsync(); } catch (Exception e) { MessageBox.Show(e.Message); } }
    private async Task PersistirAsync() { if (ClienteSelecionado is null) throw new ArgumentException("Selecione um cliente para salvar o orçamento."); foreach (var item in Itens) Validar(item); var salvo = await orcamentos.SalvarOrcamentoAsync(new(OrcamentoId, ClienteSelecionado.ClienteId, ClienteSelecionado.VeiculoId, Status, Itens.Select(x => x.ToDto()).ToList())); OrcamentoId = salvo.Id; Alterado = false; }
    private void ItensAlterados(object? sender, NotifyCollectionChangedEventArgs e) { if (e.OldItems is not null) foreach (ItemOrcamentoViewModel item in e.OldItems) item.PropertyChanged -= ItemAlterado; if (e.NewItems is not null) foreach (ItemOrcamentoViewModel item in e.NewItems) item.PropertyChanged += ItemAlterado; RecalcularSeguro(); MarcarAlterado(); }
    private void ItemAlterado(object? sender, PropertyChangedEventArgs e) { RecalcularSeguro(); MarcarAlterado(); }
    private void RecalcularSeguro()
    {
        try
        {
            MensagemValidacao = "";
            Recalcular();
        }
        catch (ArgumentException ex)
        {
            MensagemValidacao = ex.Message;
            Totais = TotaisOrcamento.Zero;
        }
    }
    private void Recalcular() => Totais = CalculadoraOrcamento.Calcular(Itens.Select(x => new OrcamentoItem { Descricao = x.Descricao, Quantidade = x.Quantidade, ValorUnitario = x.ValorUnitario, DescontoUnitario = x.DescontoUnitario }));
    private static void Validar(ItemOrcamentoViewModel item) => CalculadoraOrcamento.Validar(new OrcamentoItem { Descricao = item.Descricao, Quantidade = item.Quantidade, ValorUnitario = item.ValorUnitario, DescontoUnitario = item.DescontoUnitario });
    private void MarcarAlterado() { if (!carregando) Alterado = true; }
}
