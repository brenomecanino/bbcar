using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AutoOrcamento.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609070002_AddOrcamentoNumero")]
public partial class AddOrcamentoNumero : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
CREATE SEQUENCE orcamentos_numero_seq;
ALTER TABLE orcamentos ADD COLUMN numero bigint;
UPDATE orcamentos SET numero = nextval('orcamentos_numero_seq') WHERE numero IS NULL;
ALTER TABLE orcamentos ALTER COLUMN numero SET DEFAULT nextval('orcamentos_numero_seq');
ALTER TABLE orcamentos ALTER COLUMN numero SET NOT NULL;
ALTER SEQUENCE orcamentos_numero_seq OWNED BY orcamentos.numero;
CREATE UNIQUE INDEX ux_orcamentos_numero ON orcamentos(numero);
""");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
DROP INDEX ux_orcamentos_numero;
ALTER TABLE orcamentos DROP COLUMN numero;
DROP SEQUENCE orcamentos_numero_seq;
""");
}
