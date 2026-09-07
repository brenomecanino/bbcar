using System;
using System.Windows;
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
}
