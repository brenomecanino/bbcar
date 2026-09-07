using System.Text.RegularExpressions;
using AutoOrcamento.Core.Entities;

namespace AutoOrcamento.Core.Services;

public sealed record ClienteDto(string Nome, string Placa, string Modelo, int Ano, string Telefone, string Cpf);
public sealed class ClienteComVeiculo
{
    public ClienteComVeiculo(Guid clienteId, Guid veiculoId, string nome, string placa, string modelo, int ano, string telefone, string cpf)
    { ClienteId = clienteId; VeiculoId = veiculoId; Nome = nome; Placa = placa; Modelo = modelo; Ano = ano; Telefone = telefone; Cpf = cpf; }
    public Guid ClienteId { get; }
    public Guid VeiculoId { get; }
    public string Nome { get; set; }
    public string Placa { get; set; }
    public string Modelo { get; set; }
    public int Ano { get; set; }
    public string Telefone { get; set; }
    public string Cpf { get; set; }
}
public sealed record OrcamentoItemDto(Guid? Id, string Descricao, decimal Quantidade, decimal ValorUnitario, decimal DescontoUnitario);
public sealed record OrcamentoDto(Guid? Id, Guid ClienteId, Guid VeiculoId, StatusOrcamento Status, IReadOnlyList<OrcamentoItemDto> Itens);
public sealed record OrcamentoResumoDto(Guid Id, string Placa, string Cliente, string Contato, DateTimeOffset Data, decimal ValorTotal, StatusOrcamento Status);
public sealed record OrcamentoCompletoDto(Guid Id, ClienteComVeiculo Cliente, StatusOrcamento Status, IReadOnlyList<OrcamentoItemDto> Itens, TotaisOrcamento Totais, DateTimeOffset CriadoEm);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int Total);

public interface IClienteService
{
    Task<ClienteComVeiculo?> BuscarPorPlacaAsync(string placaNormalizada);
    Task<ClienteComVeiculo> CadastrarClienteAsync(ClienteDto dto);
    Task<IReadOnlyList<ClienteComVeiculo>> ListarAsync();
    Task AtualizarAsync(ClienteComVeiculo cliente);
}

public interface IOrcamentoService
{
    Task<PagedResult<OrcamentoResumoDto>> ListarOrcamentosAsync(int page, string? filtro);
    Task<OrcamentoCompletoDto> ObterOrcamentoAsync(Guid id);
    Task<OrcamentoCompletoDto> SalvarOrcamentoAsync(OrcamentoDto dto);
}

public interface IPdfService
{
    Task<string> GerarPdfAsync(Guid orcamentoId, string? caminhoSaida);
    Task<string?> SalvarComoAsync(Guid orcamentoId);
    Task ImprimirAsync(Guid orcamentoId);
}

public static partial class Validacoes
{
    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$")]
    private static partial Regex PlacaRegex();

    [GeneratedRegex("^\\d{3}\\.\\d{3}\\.\\d{3}-\\d{2}$")]
    private static partial Regex CpfRegex();

    public static string NormalizarPlaca(string placa) => placa.Replace("-", "").Trim().ToUpperInvariant();

    public static void ValidarCliente(ClienteDto dto)
    {
        var placa = NormalizarPlaca(dto.Placa);
        if (string.IsNullOrWhiteSpace(dto.Nome) || dto.Nome.Length > 120) throw new ArgumentException("Nome inválido.");
        if (!PlacaRegex().IsMatch(placa)) throw new ArgumentException("Placa inválida.");
        if (!CpfRegex().IsMatch(dto.Cpf)) throw new ArgumentException("CPF deve usar o formato XXX.XXX.XXX-XX.");
        if (dto.Ano < 1950 || dto.Ano > DateTime.Today.Year + 1) throw new ArgumentException("Ano do veículo inválido.");
    }
}
