using System.Windows;
using AutoOrcamento.Core.Services;
using AutoOrcamento.Desktop.ViewModels;

namespace AutoOrcamento.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel vm;
    public MainWindow(MainViewModel viewModel) { InitializeComponent(); DataContext=vm=viewModel; Loaded+=async (_,_)=>await vm.CarregarAsync(); }
    private void Clientes_Click(object sender, RoutedEventArgs e) => vm.AbaSelecionada=2;
    private async void Cadastrar_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(Ano.Text,out var ano)){MessageBox.Show("Ano inválido.");return;}
        await vm.CadastrarClienteAsync(new ClienteDto(Nome.Text,Placa.Text,Modelo.Text,ano,Telefone.Text,Cpf.Text));
    }
    private void AdicionarItem_Click(object sender, RoutedEventArgs e) => DescricaoItem.Focus();
}
