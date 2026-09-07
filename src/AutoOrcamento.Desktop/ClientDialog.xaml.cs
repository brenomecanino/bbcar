using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using AutoOrcamento.Core.Services;
using AutoOrcamento.Core;

namespace AutoOrcamento.Desktop;

public partial class ClientDialog : Window
{
    public ClientDialog()
    {
        InitializeComponent();
    }

    public ClientDialog(AutoOrcamento.Core.Services.ClienteComVeiculo existing) : this()
    {
        NomeBox.Text = existing.Nome;
        PlacaBox.Text = existing.Placa;
        ModeloBox.Text = existing.Modelo;
        AnoBox.Text = existing.Ano.ToString();
        TelefoneBox.Text = existing.Telefone;
        CpfBox.Text = existing.Cpf;
    }

    public string Nome => NomeBox.Text.Trim();
    public string Placa => PlacaBox.Text.Trim();
    public string Modelo => ModeloBox.Text.Trim();
    public int Ano => int.TryParse(AnoBox.Text.Trim(), out var a) ? a : 0;
    public string Telefone => TelefoneBox.Text.Trim();
    public string Cpf => CpfBox.Text.Trim();

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // CPF masking/validation
    private static readonly Regex _digitsOnly = new("^[0-9]+$");

    private void Cpf_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !_digitsOnly.IsMatch(e.Text);
    }

    private void Cpf_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (!_digitsOnly.IsMatch(text.Replace(".", "").Replace("-", ""))) e.CancelCommand();
        }
        else e.CancelCommand();
    }

    private void Cpf_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox tb) return;
        var digits = new string(tb.Text.Where(char.IsDigit).ToArray());
        if (digits.Length > 11) digits = digits.Substring(0, 11);
        // format as 000.000.000-00
        string formatted = digits;
        if (digits.Length <= 3) formatted = digits;
        else if (digits.Length <= 6) formatted = digits.Insert(3, ".");
        else if (digits.Length <= 9) formatted = digits.Insert(3, ".").Insert(7, ".");
        else formatted = digits.Insert(3, ".").Insert(7, ".").Insert(11, "-");
        var sel = tb.SelectionStart;
        tb.Text = formatted;
        tb.SelectionStart = Math.Min(tb.Text.Length, sel + 1);
    }
}
