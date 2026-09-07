# SPEC — Sistema de Orçamentos de Manutenção Veicular (AutoOrçamento)

**Versão da spec:** 1.0
**Data:** 2026-09-07
**Alvo de execução:** Agente AI (coding agent) — esta spec é auto-contida e deve ser seguida literalmente.

---

## 1. Visão geral

Aplicação **desktop Windows** para oficinas mecânicas: cadastro de clientes/veículos, criação e edição de orçamentos de manutenção veicular, com geração de PDF e impressão.

Stack obrigatória:
- **.NET 8** (LTS)
- **PostgreSQL 14+** (via Npgsql + EF Core 8)
- **Frontend desktop:** WPF (Windows Presentation Foundation) — recomendado para produtividade com DataGrid e bindings. Alternativa aceita: WinForms.
- **PDF:** QuestPDF (MIT, geração nativa em código, sem dependências externas)
- **Impressão:** abrir o PDF no visualizador padrão do Windows com diálogo de impressão (`ProcessStartInfo` com `Verb = "print"`) — simples e confiável. Alternativa: impressão direta via `PrintDocument`.

> Decisão de arquitetura: "backend + frontend integrado" = **um único processo desktop**. O backend é uma camada de serviços (class library `AutoOrcamento.Core`) consumida diretamente pelo WPF, sem HTTP. Separação em camadas (UI → Services → Data) obrigatória para permitir futura extração da API.

---

## 2. Estrutura da solução

```
AutoOrcamento.sln
├── src/
│   ├── AutoOrcamento.Core/          (entidades, interfaces, regras de negócio)
│   │   ├── Entities/                (Cliente, Veiculo, Orcamento, OrcamentoItem)
│   │   ├── Services/                (IOrcamentoService, IClienteService, IPdfService)
│   │   └── CalculadoraOrcamento.cs  (regras de cálculo — pura, testável)
│   ├── AutoOrcamento.Data/          (EF Core + Npgsql)
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/          (Fluent API, índices, constraints)
│   │   ├── Migrations/
│   │   └── Repositories/
│   ├── AutoOrcamento.Desktop/       (WPF)
│   │   ├── Views/                   (MainWindow, NovoOrcamentoView, ClienteCadastroView, Dialogs)
│   │   ├── ViewModels/              (MVVM, CommunityToolkit.Mvvm)
│   │   └── Services/                (implementações WPF: PdfService, DialogService)
│   └── AutoOrcamento.Tests/         (xUnit — cálculos + integração com Testcontainers/pg)
└── README.md                        (como rodar: connection string, migrations, etc.)
```

---

## 3. Modelo de dados (PostgreSQL)

### 3.1 Tabelas

**clientes**
| coluna | tipo | obrigatório | notas |
|---|---|---|---|
| id | UUID PK | sim | default `gen_random_uuid()` |
| nome | VARCHAR(120) | sim | |
| telefone | VARCHAR(20) | sim | |
| cpf | VARCHAR(14) | sim | validar formato XXX.XXX.XXX-XX; único |
| criado_em | TIMESTAMPTZ | sim | default now() |

**veiculos**
| coluna | tipo | obrigatório | notas |
|---|---|---|---|
| id | UUID PK | sim | |
| cliente_id | UUID FK → clientes.id | sim | ON DELETE RESTRICT |
| placa | VARCHAR(8) | sim | única, normalizar MAIÚSCULAS sem hífen; índice único |
| modelo | VARCHAR(80) | sim | |
| ano | INT | sim | entre 1950 e ano corrente + 1 |

**orcamentos**
| coluna | tipo | obrigatório | notas |
|---|---|---|---|
| id | UUID PK | sim | |
| cliente_id | UUID FK → clientes.id | sim | ON DELETE RESTRICT |
| status | VARCHAR(12) | sim | CHECK IN ('pendente','aprovado','recusado'); default 'pendente' |
| total_sem_desconto | NUMERIC(12,2) | sim | persistido (snapshot) |
| total_desconto | NUMERIC(12,2) | sim | |
| total_com_desconto | NUMERIC(12,2) | sim | |
| criado_em | TIMESTAMPTZ | sim | default now() |
| atualizado_em | TIMESTAMPTZ | sim | trigger ou set manual no app |

**orcamento_itens**
| coluna | tipo | obrigatório | notas |
|---|---|---|---|
| id | UUID PK | sim | |
| orcamento_id | UUID FK → orcamentos.id | sim | ON DELETE CASCADE |
| descricao | VARCHAR(200) | sim | serviço/peça/ texto livre |
| quantidade | NUMERIC(10,2) | sim | > 0 |
| valor_unitario | NUMERIC(12,2) | sim | >= 0 |
| desconto_unitario | NUMERIC(12,2) | sim | >= 0 e <= valor_unitario |

### 3.2 Índices
- `veiculos.placa` único (busca por placa é o caminho crítico do sistema).
- `orcamentos(criado_em DESC)` para a listagem inicial.
- FKs com índices por padrão.

---

## 4. Regras de negócio

### 4.1 Cálculo (classe pura `CalculadoraOrcamento`, coberta por testes)
Para cada item: `subtotal_item = quantidade × valor_unitario`; `desconto_item = quantidade × desconto_unitario`.

- **Total sem desconto** = Σ subtotal_item
- **Total desconto** = Σ desconto_item
- **Total com desconto** = Total sem desconto − Total desconto
- **Percentual de desconto** = (Total desconto / Total sem desconto) × 100 — exibir 2 casas; se Total sem desconto = 0 → exibir 0,00%
- Todos os valores monetários com 2 casas decimais (arredondamento `MidpointRounding.AwayFromZero`). Nunca usar `float/double` para dinheiro — usar `decimal`.

### 4.2 Cliente e orçamento
- Todo orçamento **exige um cliente/veículo atrelado** — regra de salvamento e de impressão.
- Busca de cliente é feita **pela placa** (ex.: usuário digita `abc1d23` → normaliza para `ABC1D23` e localiza).
- Placa não localizada → exibir mensagem **"Cliente não localizado"** e abrir automaticamente a tela de cadastro de cliente (nome, placa, modelo, ano, telefone, cpf). Ao concluir o cadastro, retornar à tela de orçamento com o novo cliente já selecionado.
- Orçamento novo nasce com status **pendente**. Edição permite alterar status (pendente/aprovado/recusado).
- Itens de orçamento **somem** (snapshot de totais é persistido). Itens ficam guardados para reimpressão.

---

## 5. Telas e fluxos

### 5.1 Tela principal (`MainWindow`)
**Barra superior — 2 botões:** `+ Novo Orçamento` | `Clientes`

**Tabela de últimos orçamentos** (DataGrid), ordenada por **mais recente primeiro** (`criado_em DESC`), com colunas:

| Placa | Cliente | Contato | Data | Valor Total | Status | Ações |
|---|---|---|---|---|---|---|
| ABC1D23 | João Silva | (11) 99999-0000 | 07/09/2026 | R$ 850,00 | [pendente] | [Editar] [Imprimir] |

Requisitos:
- Status como badge colorido: pendente = âmbar, aprovado = verde, recusado = vermelho.
- Filtro rápido (campo de texto) por placa ou nome do cliente — desejável, não bloqueante.
- **Editar** → abre a tela de orçamento em modo edição (todos os campos editáveis, incl. status).
- **Imprimir** → gera/regenera o PDF do orçamento e envia à impressão.
- Botão **Clientes** → tela de listagem/cadastro/edição de clientes (CRUD simples: nome, placa, modelo, ano, telefone, cpf).
- Paginação ou scroll virtual se > 500 linhas; carregar inicialmente os últimos 100.

### 5.2 Tela de Novo Orçamento / Edição
**Cabeçalho**
- Campo **Cliente**: entrada pela **placa do veículo** + botão buscar. Ao localizar, exibe `Nome — Modelo/Ano` e bloqueia o campo (botão trocar para rebuscar).
- Em edição, campos de cliente vêm preenchidos e desabilitados.

**Formulário de item** (uma linha de inputs):
| Descrição (serviço/peça, texto livre, **máx. 200 caracteres**) | Quantidade | Valor unitário (R$) | Desconto unitário (R$) | [+] |
- Validação antes de adicionar: descrição não vazia e ≤ 200; quantidade > 0; valor ≥ 0; desconto ≥ 0 e ≤ valor unitário.
- Botão **+** → adiciona item à tabela e limpa os campos, mantendo foco no campo Descrição.

**Tabela de itens** — **tamanho fixo na tela** (altura fixa, ex.: 300px) com **scroll interno** conforme itens são adicionados. Colunas: Descrição | Qtd | Vlr Unit | Desc Unit | Subtotal | [remover ✕].

**Rodapé da tela (abaixo da tabela):**
- `Total sem desconto: R$ X.XXX,XX`
- `Total com desconto: R$ X.XXX,XX`
- `Desconto: R$ XXX,XX (XX,XX%)`
- Atualizados **ao vivo** a cada item adicionado/removido/editado.

**Botões:** `Salvar` | `Imprimir`

### 5.3 Regras Salvar / Imprimir
- Sem cliente atrelado → bloquear e exibir: *"Selecione um cliente para salvar o orçamento."*
- Sem itens → bloquear: *"Adicione ao menos um item ao orçamento."*
- `Salvar` → persiste (insert ou update) e retorna à tela principal.
- `Imprimir` → salva (se novo/alterado) **e** gera o PDF **e** dispara a impressão. Em modo de edição, perguntar "Salvar alterações antes de imprimir?" se houver mudanças não salvas.

### 5.4 PDF do orçamento (QuestPDF)
Layout A4:
1. Cabeçalho: nome da oficina (configurável em `appsettings.json`), título "ORÇAMENTO", nº do orçamento (sequencial/UUID curto), data.
2. Dados do cliente: nome, telefone, CPF; veículo: placa, modelo, ano.
3. Tabela de itens (Descrição, Qtd, Vlr Unit, Desc, Subtotal).
4. Totais: sem desconto, desconto (+%), com desconto (destaque).
5. Rodapé: status do orçamento, linha de assinatura do cliente, observação de validade do orçamento (ex.: 15 dias).
- Nome do arquivo: `orcamento_{placa}_{yyyyMMdd_HHmm}.pdf` em `%TEMP%` ou pasta configurável.
- Salvar em disco com opção "Salvar como" (SaveFileDialog) e imprimir — ambas disponíveis.

---

## 6. Endpoints/camada de serviço (contratos)

Como o backend é in-process, "API" = interfaces de serviço. Contratos mínimos:

```csharp
Task<ClienteComVeiculo?> BuscarPorPlacaAsync(string placaNormalizada);
Task<ClienteComVeiculo> CadastrarClienteAsync(ClienteDto dto);        // valida CPF/placa duplicados
Task<PagedResult<OrcamentoResumoDto>> ListarOrcamentosAsync(int page, string? filtro);
Task<OrcamentoCompletoDto> ObterOrcamentoAsync(Guid id);
Task<OrcamentoCompletoDto> SalvarOrcamentoAsync(OrcamentoDto dto);    // insert ou update (upsert por id)
Task<string> GerarPdfAsync(Guid orcamentoId, string? caminhoSaida);   // retorna caminho do arquivo
Task ImprimirAsync(Guid orcamentoId);
```

---

## 7. Configuração e execução

- Connection string em `appsettings.json` + override por variável de ambiente `ConnectionStrings__Default`:
  `"Host=localhost;Database=auto_orcamento;Username=postgres;Password=postgres"`
- `dotnet ef database update` (ou migrate automática ao iniciar, `Migrate()` no startup — preferido para desktop).
- Aplicativo inicia com a tela principal já listando os últimos orçamentos.

---

## 8. Critérios de aceite (Definition of Done)

1. Abrir o sistema → tabela com os últimos orçamentos ordenados do mais recente ao mais antigo, com placa, cliente, contato, data, valor total e badge de status (pendente/aprovado/recusado), e ações Editar/Imprimir por linha.
2. Botões superiores funcionando: `Novo Orçamento` e `Clientes`.
3. Em Novo Orçamento: busca de cliente por placa; placa inexistente → mensagem "Cliente não localizado" + abertura automática da tela de cadastro (nome, placa, modelo, ano, telefone, cpf); após cadastro, cliente vinculado ao orçamento.
4. Adição dinâmica de itens via botão + (descrição ≤ 200 chars, qtd, valor unit., desconto unit.), tabela com tamanho fixo e scroll.
5. Rodapé exibindo corretamente e ao vivo: total sem desconto, total com desconto, desconto em R$ + percentual.
6. Salvar bloqueado sem cliente/sem itens; imprimir salva + gera PDF + envia à impressora.
7. Editar recarrega orçamento completo e permite alterar itens, dados e status.
8. PDF gerado com layout da seção 5.4 e valores corretos (validar com cálculo manual).
9. Testes unitários passando para `CalculadoraOrcamento` (casos: sem desconto, com desconto, desconto total, arredondamento, percentual).
10. README com instruções de build, migration e execução no Windows.

---

## 9. Fora de escopo (v1)

- Autenticação/usuários, multi-oficina.
- Ordem de serviço (transformar orçamento em OS).
- Envio de PDF por e-mail/WhatsApp.
- Relatórios gerenciais.

---

## 10. Casos de teste de exemplo (para o agente validar)

| # | Cenário | Entrada | Esperado |
|---|---|---|---|
| 1 | Item sem desconto | 2 × R$ 100,00, desc 0 | Sem desc: 200,00; desc: 0,00 (0%); com desc: 200,00 |
| 2 | Item com desconto | 2 × R$ 100,00, desc unit 10 | Sem desc: 200,00; desc: 20,00 (10%); com desc: 180,00 |
| 3 | Misto | 2×100 desc10 + 1×50 desc0 | Sem desc: 250; desc: 20 (8%); com desc: 230 |
| 4 | Desconto = valor | 1 × R$ 80, desc 80 | Com desc: 0,00; percentual 100% |
| 5 | Desconto inválido | desc > valor unit | Recusado com mensagem |
| 6 | Salvar sem cliente | — | Bloqueado, mensagem de cliente obrigatório |
| 7 | Placa não existe | "ZZZ9999" | "Cliente não localizado" + tela de cadastro |
