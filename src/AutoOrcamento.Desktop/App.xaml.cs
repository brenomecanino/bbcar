using System.Windows;
using AutoOrcamento.Core.Services;
using AutoOrcamento.Data;
using AutoOrcamento.Desktop.Services;
using AutoOrcamento.Desktop.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestPDF.Infrastructure;

namespace AutoOrcamento.Desktop;

public partial class App : Application
{
    private IHost? host;
    protected override async void OnStartup(StartupEventArgs e)
    {
        QuestPDF.Settings.License=LicenseType.Community;
        host=Host.CreateDefaultBuilder().ConfigureAppConfiguration(c=>c.AddJsonFile("appsettings.json",false).AddEnvironmentVariables()).ConfigureServices((c,s)=>{s.AddDbContext<AppDbContext>(o=>o.UseNpgsql(c.Configuration.GetConnectionString("Default")));s.AddScoped<IClienteService,ClienteService>();s.AddScoped<IOrcamentoService,OrcamentoService>();s.AddScoped<IPdfService,PdfService>();s.AddTransient<MainViewModel>();s.AddTransient<MainWindow>();}).Build();
        await host.StartAsync();
        try { using var scope=host.Services.CreateScope();await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(); }
        catch(Exception ex){MessageBox.Show($"Não foi possível conectar ao PostgreSQL: {ex.Message}","Banco de dados");}
        host.Services.GetRequiredService<MainWindow>().Show();
    }
    protected override async void OnExit(ExitEventArgs e){if(host is not null){await host.StopAsync();host.Dispose();}base.OnExit(e);}
}
