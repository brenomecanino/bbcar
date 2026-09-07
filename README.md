# AutoOrçamento

Aplicativo Windows WPF para clientes, veículos e orçamentos com PDF e impressão.

## Requisitos

- Windows 10/11 com SDK/runtime .NET 8
- PostgreSQL 14+

## Configuração

Edite `src/AutoOrcamento.Desktop/appsettings.json` ou defina `ConnectionStrings__Default`:

```powershell
$env:ConnectionStrings__Default="Host=localhost;Database=auto_orcamento;Username=postgres;Password=postgres"
```

A migration é aplicada ao iniciar. Para aplicação manual:

```powershell
dotnet tool install --global dotnet-ef --version 8.*
dotnet ef database update --project src/AutoOrcamento.Data --startup-project src/AutoOrcamento.Desktop
```

## Build, testes e execução

```powershell
dotnet restore AutoOrcamento.sln
dotnet build AutoOrcamento.sln
dotnet test AutoOrcamento.sln
dotnet run --project src/AutoOrcamento.Desktop
```

A impressão usa o visualizador de PDF padrão do Windows. PDFs temporários são gravados em `%TEMP%`.
