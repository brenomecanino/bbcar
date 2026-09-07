using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AutoOrcamento.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609070001_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE TABLE clientes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), nome varchar(120) NOT NULL, telefone varchar(20) NOT NULL, cpf varchar(14) NOT NULL UNIQUE, criado_em timestamptz NOT NULL DEFAULT now());
CREATE TABLE veiculos (id uuid PRIMARY KEY, cliente_id uuid NOT NULL REFERENCES clientes(id) ON DELETE RESTRICT, placa varchar(8) NOT NULL UNIQUE, modelo varchar(80) NOT NULL, ano int NOT NULL CHECK (ano >= 1950));
CREATE INDEX ix_veiculos_cliente_id ON veiculos(cliente_id);
CREATE TABLE orcamentos (id uuid PRIMARY KEY, cliente_id uuid NOT NULL REFERENCES clientes(id) ON DELETE RESTRICT, veiculo_id uuid NOT NULL REFERENCES veiculos(id) ON DELETE RESTRICT, status varchar(12) NOT NULL DEFAULT 'pendente' CHECK (status IN ('pendente','aprovado','recusado')), total_sem_desconto numeric(12,2) NOT NULL, total_desconto numeric(12,2) NOT NULL, total_com_desconto numeric(12,2) NOT NULL, criado_em timestamptz NOT NULL DEFAULT now(), atualizado_em timestamptz NOT NULL DEFAULT now());
CREATE INDEX ix_orcamentos_cliente_id ON orcamentos(cliente_id);
CREATE INDEX ix_orcamentos_veiculo_id ON orcamentos(veiculo_id);
CREATE INDEX ix_orcamentos_criado_em ON orcamentos(criado_em DESC);
CREATE TABLE orcamento_itens (id uuid PRIMARY KEY, orcamento_id uuid NOT NULL REFERENCES orcamentos(id) ON DELETE CASCADE, descricao varchar(200) NOT NULL, quantidade numeric(10,2) NOT NULL CHECK (quantidade > 0), valor_unitario numeric(12,2) NOT NULL CHECK (valor_unitario >= 0), desconto_unitario numeric(12,2) NOT NULL CHECK (desconto_unitario >= 0 AND desconto_unitario <= valor_unitario));
CREATE INDEX ix_itens_orcamento_id ON orcamento_itens(orcamento_id);
""");
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("DROP TABLE orcamento_itens; DROP TABLE orcamentos; DROP TABLE veiculos; DROP TABLE clientes;");
}
