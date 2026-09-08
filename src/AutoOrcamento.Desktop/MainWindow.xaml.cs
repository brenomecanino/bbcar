using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Globalization;
using AutoOrcamento.Core.Services;
using AutoOrcamento.Desktop.ViewModels;

namespace AutoOrcamento.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel vm;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = vm = viewModel;
        Loaded += async (_, _) => await vm.CarregarAsync();
    }

    private void Clientes_Click(object sender, RoutedEventArgs e) => vm.AbaSelecionada = 2;

    // O método Cadastrar_Click referenciava controles que não existem na janela principal
    // Mantido intencionalmente vazio para evitar referências inválidas.
    private async void Cadastrar_Click(object sender, RoutedEventArgs e) { await System.Threading.Tasks.Task.CompletedTask; }

    private void AdicionarItem_Click(object sender, RoutedEventArgs e)
    {
        ValorUnitarioBox.Clear();
        DescontoUnitarioBox.Clear();
        DescricaoItem.Focus();
    }

    private void EditarCliente_Click(object sender, RoutedEventArgs e)
    {
        // Abre diálogo de edição para o cliente selecionado
        if (vm.ClienteSelecionado is null)
        {
            MessageBox.Show("Selecione um cliente para editar.");
            return;
        }

        var dlg = new ClientDialog(vm.ClienteSelecionado);
        dlg.Owner = this;
        if (dlg.ShowDialog() == true)
        {
            var c = vm.ClienteSelecionado;
            var updated = new AutoOrcamento.Core.Services.ClienteComVeiculo(c.ClienteId, c.VeiculoId, dlg.Nome, dlg.Placa, dlg.Modelo, dlg.Ano, dlg.Telefone, dlg.Cpf);
            _ = vm.AtualizarClienteAsync(updated);
        }
    }

    private void NovoCliente_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ClientDialog();
        dlg.Owner = this;
        if (dlg.ShowDialog() == true)
        {
            var dto = new AutoOrcamento.Core.Services.ClienteDto(dlg.Nome, dlg.Placa, dlg.Modelo, dlg.Ano, dlg.Telefone, dlg.Cpf);
            _ = vm.CadastrarClienteAsync(dto);
        }
    }

    // Quantidade: aceitar apenas dígitos
    private static readonly Regex _digitsOnly = new("^[0-9]+$");
    private static readonly Regex _moneyInput = new("^[0-9]*(,[0-9]{0,2})?$");
    private static readonly CultureInfo _brazilianCulture = CultureInfo.GetCultureInfo("pt-BR");
    private void Quantidade_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !_digitsOnly.IsMatch(e.Text);
    }

    private void Quantidade_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (!_digitsOnly.IsMatch(text)) e.CancelCommand();
        }
        else e.CancelCommand();
    }

    private void DigitsOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !_digitsOnly.IsMatch(e.Text);
    }

    private void DigitsOnly_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (!_digitsOnly.IsMatch(text)) e.CancelCommand();
        }
        else e.CancelCommand();
    }

    private void Money_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox) return;
        var proposed = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
            .Insert(textBox.SelectionStart, e.Text);
        e.Handled = !_moneyInput.IsMatch(proposed);
    }

    private void Money_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox ||
            !e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var pasted = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        var proposed = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
            .Insert(textBox.SelectionStart, pasted);
        if (!_moneyInput.IsMatch(proposed)) e.CancelCommand();
    }

    private void MoneyDigits_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox tb) return;
        var text = tb.Text?.Trim() ?? string.Empty;
        var error = tb.Name == "ValorUnitarioBox" ? ValorError : DescontoError;

        if (string.IsNullOrEmpty(text))
        {
            error.Visibility = Visibility.Collapsed;
            SetMoneyValue(tb, 0m);
            return;
        }

        if (decimal.TryParse(text, NumberStyles.AllowDecimalPoint, _brazilianCulture, out var value) &&
            _moneyInput.IsMatch(text))
        {
            error.Visibility = Visibility.Collapsed;
            SetMoneyValue(tb, value);
        }
        else
        {
            error.Visibility = Visibility.Visible;
        }
    }

    private void SetMoneyValue(System.Windows.Controls.TextBox textBox, decimal value)
    {
        if (textBox.Name == "ValorUnitarioBox") vm.ValorUnitario = value;
        else if (textBox.Name == "DescontoUnitarioBox") vm.DescontoUnitario = value;
    }
    private void Quantidade_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox tb) return;
        var text = tb.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            QuantidadeError.Visibility = Visibility.Collapsed;
            vm.Quantidade = 0;
            return;
        }

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        {
            QuantidadeError.Visibility = Visibility.Collapsed;
            vm.Quantidade = n;
        }
        else
        {
            QuantidadeError.Visibility = Visibility.Visible;
        }
    }

    // Title bar handlers
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            if (e.ClickCount == 2) ToggleWindowState();
            else DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleWindowState();

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
}
