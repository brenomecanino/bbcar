using AutoOrcamento.Core;
using AutoOrcamento.Core.Entities;
using AutoOrcamento.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace AutoOrcamento.Data;

public sealed class ClienteService(AppDbContext db) : IClienteService
{
    public async Task<ClienteComVeiculo?> BuscarPorPlacaAsync(string placaNormalizada) => await db.Veiculos.AsNoTracking().Include(v => v.Cliente).Where(v => v.Placa == Validacoes.NormalizarPlaca(placaNormalizada)).Select(v => Map(v)).SingleOrDefaultAsync();

    public async Task<ClienteComVeiculo> CadastrarClienteAsync(ClienteDto dto)
    {
        Validacoes.ValidarCliente(dto);
        var placa = Validacoes.NormalizarPlaca(dto.Placa);
        if (await db.Clientes.AnyAsync(x => x.Cpf == dto.Cpf)) throw new InvalidOperationException("CPF já cadastrado.");
        if (await db.Veiculos.AnyAsync(x => x.Placa == placa)) throw new InvalidOperationException("Placa já cadastrada.");
        var cliente = new Cliente { Nome = dto.Nome.Trim(), Telefone = dto.Telefone.Trim(), Cpf = dto.Cpf };
        var veiculo = new Veiculo { Cliente = cliente, ClienteId = cliente.Id, Placa = placa, Modelo = dto.Modelo.Trim(), Ano = dto.Ano };
        db.AddRange(cliente, veiculo);
        await db.SaveChangesAsync();
        return Map(veiculo);
    }

    public async Task<IReadOnlyList<ClienteComVeiculo>> ListarAsync() => await db.Veiculos.AsNoTracking().Include(v => v.Cliente).OrderBy(v => v.Cliente.Nome).Select(v => Map(v)).ToListAsync();

    public async Task AtualizarAsync(ClienteComVeiculo dto)
    {
        var atual = await db.Veiculos.Include(x => x.Cliente).SingleAsync(x => x.Id == dto.VeiculoId && x.ClienteId == dto.ClienteId);
        var placa = Validacoes.NormalizarPlaca(dto.Placa);
        Validacoes.ValidarCliente(new ClienteDto(dto.Nome, placa, dto.Modelo, dto.Ano, dto.Telefone, dto.Cpf));
        if (await db.Clientes.AnyAsync(x => x.Cpf == dto.Cpf && x.Id != dto.ClienteId)) throw new InvalidOperationException("CPF já cadastrado.");
        if (await db.Veiculos.AnyAsync(x => x.Placa == placa && x.Id != dto.VeiculoId)) throw new InvalidOperationException("Placa já cadastrada.");
        atual.Placa = placa;
        atual.Modelo = dto.Modelo.Trim();
        atual.Ano = dto.Ano;
        atual.Cliente.Nome = dto.Nome.Trim();
        atual.Cliente.Telefone = dto.Telefone.Trim();
        atual.Cliente.Cpf = dto.Cpf;
        await db.SaveChangesAsync();
    }

    private static ClienteComVeiculo Map(Veiculo v) => new(v.ClienteId, v.Id, v.Cliente.Nome.ToUpperInvariant(), v.Placa.ToUpperInvariant(), v.Modelo.ToUpperInvariant(), v.Ano, v.Cliente.Telefone, v.Cliente.Cpf);
}

public sealed class OrcamentoService(AppDbContext db) : IOrcamentoService
{
    public async Task<PagedResult<OrcamentoResumoDto>> ListarOrcamentosAsync(int page, string? filtro, DateTime? dataInicial, DateTime? dataFinal)
    {
        var q = db.Orcamentos.AsNoTracking().Include(x => x.Cliente).Include(x => x.Veiculo).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            var f = filtro.Trim().ToUpperInvariant();
            q = q.Where(x => x.Veiculo.Placa.Contains(f) || x.Cliente.Nome.ToUpper().Contains(f) || x.Cliente.Telefone.Contains(f));
        }
        if (dataInicial is { } inicial) q = q.Where(x => x.CriadoEm >= inicial);
        if (dataFinal is { } final) q = q.Where(x => x.CriadoEm < final.AddDays(1));
        var total = await q.CountAsync();
        var pagina = Math.Max(1, page);
        var items = await q.OrderByDescending(x => x.CriadoEm).Skip((pagina - 1) * 100).Take(100).Select(x => new OrcamentoResumoDto(x.Id, x.Veiculo.Placa.ToUpper(), x.Cliente.Nome.ToUpper(), x.Cliente.Telefone, x.CriadoEm, x.TotalComDesconto, x.Status)).ToListAsync();
        return new(items, pagina, total);
    }

    public async Task<OrcamentoCompletoDto> ObterOrcamentoAsync(Guid id) => Map(await db.Orcamentos.AsNoTracking().Include(x => x.Cliente).Include(x => x.Veiculo).Include(x => x.Itens).SingleAsync(x => x.Id == id));

    public async Task<OrcamentoCompletoDto> SalvarOrcamentoAsync(OrcamentoDto dto)
    {
        if (dto.ClienteId == Guid.Empty || dto.VeiculoId == Guid.Empty) throw new ArgumentException("Selecione um cliente para salvar o orçamento.");
        if (dto.Itens.Count == 0) throw new ArgumentException("Adicione ao menos um item ao orçamento.");
        if (await db.Veiculos.SingleOrDefaultAsync(x => x.Id == dto.VeiculoId && x.ClienteId == dto.ClienteId) is null) throw new ArgumentException("Cliente e veículo não correspondem.");
        var entities = dto.Itens.Select(i => new OrcamentoItem { Id = Guid.NewGuid(), OrcamentoId = dto.Id ?? Guid.Empty, Descricao = i.Descricao, Quantidade = i.Quantidade, ValorUnitario = i.ValorUnitario, DescontoUnitario = i.DescontoUnitario }).ToList();
        var t = CalculadoraOrcamento.Calcular(entities);
        Orcamento o;
        if (dto.Id is { } id)
        {
            o = await db.Orcamentos.Include(x => x.Itens).SingleAsync(x => x.Id == id);
            o.ClienteId = dto.ClienteId;
            o.VeiculoId = dto.VeiculoId;
            db.OrcamentoItens.RemoveRange(o.Itens);
            o.Itens.Clear();
            foreach (var item in entities) { item.OrcamentoId = o.Id; o.Itens.Add(item); }
        }
        else
        {
            o = new Orcamento { ClienteId = dto.ClienteId, VeiculoId = dto.VeiculoId };
            foreach (var item in entities) { item.OrcamentoId = o.Id; o.Itens.Add(item); }
            db.Orcamentos.Add(o);
        }
        o.Status = dto.Status;
        o.TotalSemDesconto = t.TotalSemDesconto;
        o.TotalDesconto = t.TotalDesconto;
        o.TotalComDesconto = t.TotalComDesconto;
        o.AtualizadoEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return await ObterOrcamentoAsync(o.Id);
    }

    private static OrcamentoCompletoDto Map(Orcamento o) => new(o.Id, new(o.ClienteId, o.VeiculoId, o.Cliente.Nome, o.Veiculo.Placa, o.Veiculo.Modelo, o.Veiculo.Ano, o.Cliente.Telefone, o.Cliente.Cpf), o.Status, o.Itens.Select(i => new OrcamentoItemDto(i.Id, i.Descricao, i.Quantidade, i.ValorUnitario, i.DescontoUnitario)).ToList(), new(o.TotalSemDesconto, o.TotalDesconto, o.TotalComDesconto, o.TotalSemDesconto == 0 ? 0 : Math.Round(o.TotalDesconto / o.TotalSemDesconto * 100, 2, MidpointRounding.AwayFromZero)), o.CriadoEm);
}
