-- Script temporário para testes de paginação.
-- Execute no banco auto_orcamento após aplicar as migrations.
-- Os registros usam o prefixo TESTE_PAGINACAO_ para facilitar a exclusão manual.
-- Não faz parte da aplicação e pode ser removido após o uso.

BEGIN;

CREATE TEMP TABLE dados_teste_paginacao ON COMMIT DROP AS
SELECT
    g,
    gen_random_uuid() AS cliente_id,
    gen_random_uuid() AS veiculo_id,
    gen_random_uuid() AS orcamento_id,
    gen_random_uuid() AS item_id,
    (CURRENT_DATE - (g - 1)::int) + TIME '12:00:00' AS criado_em
FROM generate_series(1, 100) AS g;

INSERT INTO clientes (id, nome, telefone, cpf, criado_em)
SELECT
    cliente_id,
    'TESTE_PAGINACAO_' || LPAD(g::text, 3, '0'),
    '1199' || LPAD(g::text, 7, '0'),
    '900.' || LPAD(g::text, 3, '0') || '.900-00',
    criado_em
FROM dados_teste_paginacao;

INSERT INTO veiculos (id, cliente_id, placa, modelo, ano)
SELECT
    veiculo_id,
    cliente_id,
    'T' || LPAD(g::text, 3, '0') || 'A' || LPAD((g % 10)::text, 1, '0'),
    'MODELO TESTE PAGINACAO',
    2020
FROM dados_teste_paginacao;

INSERT INTO orcamentos (id, cliente_id, veiculo_id, status, total_sem_desconto, total_desconto, total_com_desconto, criado_em, atualizado_em)
SELECT
    orcamento_id,
    cliente_id,
    veiculo_id,
    CASE g % 3 WHEN 0 THEN 'aprovado' WHEN 1 THEN 'pendente' ELSE 'recusado' END,
    100.00,
    0.00,
    100.00,
    criado_em,
    criado_em
FROM dados_teste_paginacao;

INSERT INTO orcamento_itens (id, orcamento_id, descricao, quantidade, valor_unitario, desconto_unitario)
SELECT
    item_id,
    orcamento_id,
    'ITEM TESTE PAGINACAO',
    1,
    100.00,
    0.00
FROM dados_teste_paginacao;

COMMIT;

-- Conferência: deve retornar 100.
SELECT COUNT(*) AS total_registros_teste
FROM orcamentos o
JOIN clientes c ON c.id = o.cliente_id
WHERE c.nome LIKE 'TESTE_PAGINACAO_%';

-- Exclusão manual sugerida após os testes:
-- BEGIN;
-- DELETE FROM orcamento_itens
-- WHERE orcamento_id IN (
--     SELECT o.id FROM orcamentos o
--     JOIN clientes c ON c.id = o.cliente_id
--     WHERE c.nome LIKE 'TESTE_PAGINACAO_%'
-- );
-- DELETE FROM orcamentos
-- WHERE cliente_id IN (SELECT id FROM clientes WHERE nome LIKE 'TESTE_PAGINACAO_%');
-- DELETE FROM veiculos
-- WHERE cliente_id IN (SELECT id FROM clientes WHERE nome LIKE 'TESTE_PAGINACAO_%');
-- DELETE FROM clientes WHERE nome LIKE 'TESTE_PAGINACAO_%';
-- COMMIT;
