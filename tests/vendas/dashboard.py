from decimal import Decimal


def executar_dashboard(portas, sql, cliente_http, token_a, token_b, token_vendas):
    _, venda = cliente_http(portas["MS-Venda"], "/api/vendas/pedidos", token_b)
    _, compra = cliente_http(portas["MS-Compra"], "/api/compras/pedidos", token_b)
    _, estoque = cliente_http(portas["MS-Estoque"], "/api/estoque/saldos", token_b)
    _, financeiro = cliente_http(portas["MS-Financeiro"], "/api/financeiro/indicadores", token_b)

    # As datas fixas exercitam a virada de mês em São Paulo e separam o recorte dos outros testes.
    sql("""
        WITH pedidos AS (
            INSERT INTO vendas.pedidos (cliente_id, usuario_id, situacao, valor_total, criado_em, confirmado_em)
            VALUES
                (1, 1, 'CONFIRMADO', 30, now(), '2024-03-01 02:59:59+00'),
                (1, 1, 'CONFIRMADO', 70, now(), '2024-03-01 03:00:00+00'),
                (1, 1, 'CONFIRMADO', 50, now(), '2024-03-09 23:00:00+00'),
                (1, 1, 'CANCELADO', 2000, now(), '2024-03-01 04:00:00+00'),
                (1, 1, 'PENDENTE', 1000, now(), NULL)
            RETURNING id, valor_total
        )
        INSERT INTO vendas.pedidos_itens (pedido_id, produto_id, quantidade, preco_unitario, custo_unitario)
        SELECT id, CASE WHEN valor_total = 50 THEN 2 ELSE 1 END,
            CASE WHEN valor_total = 70 THEN 2 ELSE 1 END,
            CASE WHEN valor_total = 70 THEN 35 ELSE valor_total END,
            CASE WHEN valor_total = 50 THEN 7.5000 ELSE 10.2500 END
        FROM pedidos;

        INSERT INTO compras.pedidos (fornecedor_id, usuario_id, situacao, valor_total, criado_em, recebido_em)
        VALUES (1, 1, 'RECEBIDO', 60, now(), '2024-03-01 02:59:59+00'),
                (1, 1, 'RECEBIDO', 15, now(), '2024-03-01 03:00:00+00'),
                (1, 1, 'CANCELADO', 200, now(), '2024-03-10 03:00:00+00'),
                (1, 1, 'PENDENTE', 300, now(), NULL);

        INSERT INTO financeiro.titulos (tipo, venda_id, valor, vencimento, usuario_id, criado_em)
        SELECT 'RECEBER', id, 50, DATE '2024-03-15', 1, now()
        FROM vendas.pedidos WHERE valor_total = 50 AND confirmado_em = '2024-03-09 23:00:00+00';

        INSERT INTO financeiro.baixas (titulo_id, valor, forma_pagamento, usuario_id, criado_em)
        SELECT id, 10, 'PIX', 1, now() FROM financeiro.titulos WHERE vencimento = DATE '2024-03-15';

        UPDATE estoque.saldos SET estoque_minimo = quantidade + 1 WHERE produto_id = 1;
    """, "empresa_b")

    marco = "/dashboard?dataInicio=2024-03-01&dataFim=2024-03-31"
    venda(401, "GET", marco, jwt=None)
    compra(403, "GET", marco, jwt=token_vendas)
    venda(400, "GET", "/dashboard?dataInicio=2024-03-31&dataFim=2024-03-01")
    compra(400, "GET", "/dashboard?dataInicio=2024-03-01")
    assert venda(200, "GET", marco, jwt=token_a)["vendasConfirmadas"] == 0

    resumo = venda(200, "GET", marco)
    assert resumo["vendasConfirmadas"] == 2 and resumo["faturamento"] == 120
    assert resumo["lucroBruto"] == 92 and resumo["margemBruta"] == Decimal("76.67")
    assert resumo["evolucao"] == [{"mes": "2024-03-01", "valor": 120, "quantidade": 2}]
    assert [item["produtoId"] for item in resumo["produtosMaisVendidos"]] == [1, 2]
    assert [item["quantidade"] for item in resumo["produtosMaisVendidos"]] == [2, 1]
    assert [item["valor"] for item in resumo["ultimasVendas"]] == [50, 70]

    ambos = "/dashboard?dataInicio=2024-02-01&dataFim=2024-03-31"
    assert [item["mes"] for item in venda(200, "GET", ambos)["evolucao"]] == ["2024-02-01", "2024-03-01"]
    assert compra(200, "GET", marco)["totalCompras"] == 15
    assert compra(200, "GET", ambos)["totalCompras"] == 75

    titulos = financeiro(200, "GET", "?vencimentoInicio=2024-03-01&vencimentoFim=2024-03-31")
    assert titulos["contasAReceber"]["saldo"] == 40
    assert titulos["contasAReceber"]["saldoVencido"] == 40
    assert titulos["contasAReceber"]["valorBaixado"] == 10
    posicao = estoque(200, "GET", "?abaixoDoMinimo=true&pagina=1&tamanhoPagina=5")
    assert posicao["total"] == 1 and posicao["itens"][0]["produtoId"] == 1

    sql("""
        UPDATE vendas.pedidos_itens SET custo_unitario = NULL
        WHERE pedido_id IN (SELECT id FROM vendas.pedidos WHERE confirmado_em = '2024-03-09 23:00:00+00')
    """, "empresa_b")
    sem_custo = venda(200, "GET", marco)
    assert sem_custo["lucroBruto"] is None and sem_custo["margemBruta"] is None
    print("Validou faturamento, custo capturado, margem, evolução, ranking, saldo, virada de mês e permissões do Dashboard.")
