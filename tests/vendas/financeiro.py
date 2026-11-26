from concurrent.futures import ThreadPoolExecutor
from decimal import Decimal
from threading import Barrier


def executar_financeiro(portas, sql, cliente_http, token_a, token_b):
    _, compra = cliente_http(portas["MS-Compra"], "/api/compras/pedidos", token_b)
    req_venda, venda = cliente_http(portas["MS-Venda"], "/api/vendas/pedidos", token_b)
    req_fin, financeiro = cliente_http(portas["MS-Financeiro"], "/api/financeiro/titulos", token_b)

    def comprar(custo):
        return compra(201, "POST", dados={
            "fornecedorId": 1, "itens": [{"produtoId": 1, "quantidade": 1, "custoUnitario": custo}],
        })["pedido"]["id"]

    def vender(preco=40):
        return venda(201, "POST", dados={
            "clienteId": 1, "itens": [{"produtoId": 1, "quantidade": 1, "precoUnitario": preco}],
        })["pedido"]["id"]

    def titulos(filtro=""):
        return financeiro(200, "GET", filtro)["itens"]

    def indicadores():
        return financeiro(200, "GET", caminho="/api/financeiro/indicadores")

    def detalhe(id):
        return financeiro(200, "GET", f"/{id}")

    def baixar(id, valor, status=201):
        return financeiro(status, "POST", f"/{id}/baixas", {"valor": valor, "formaPagamento": "PIX"})

    def estornar(id, baixa, status=200):
        return financeiro(status, "POST", f"/{id}/baixas/{baixa}/estornar", {"motivo": " Valor incorreto "})

    def saldo():
        return venda(200, "GET", "/produtos/1/disponibilidade")["quantidadeDisponivel"]

    def concorrer(*operacoes):
        barreira = Barrier(len(operacoes))

        def chamar(operacao):
            barreira.wait(timeout=5)
            return operacao()[0]

        with ThreadPoolExecutor(max_workers=len(operacoes)) as tarefas:
            return sorted(tarefas.map(chamar, operacoes))

    financeiro(401, "GET", jwt=None)
    assert titulos() == []
    assert indicadores()["contasAPagar"]["saldo"] == 0

    gratuita = comprar(0)
    compra(200, "POST", f"/{gratuita}/receber")
    assert titulos() == []

    id_compra = comprar(100.01)
    compra(200, "POST", f"/{id_compra}/receber", {"vencimento": "2099-02-01"})
    compra(409, "POST", f"/{id_compra}/receber")
    pagar, = titulos("?tipo=PAGAR&fornecedorId=1&situacao=PENDENTE&vencimentoInicio=2099-02-01")
    assert pagar["compraId"] == id_compra and pagar["vendaId"] is None
    assert pagar["vencimento"] == "2099-02-01" and pagar["nomeFornecedor"] == "Fornecedor"
    assert pagar["valor"] == Decimal("100.01") and detalhe(pagar["id"])["baixas"] == []
    assert titulos("?tipo=PAGAR&fornecedorId=2") == []

    id_pagar = pagar["id"]
    baixar(id_pagar, 0.001, 400)
    baixar(id_pagar, 0, 400)
    financeiro(400, "POST", f"/{id_pagar}/baixas", {"valor": 1, "formaPagamento": "CHEQUE"})
    primeira = baixar(id_pagar, 40.01)
    assert titulos("?situacao=PARCIAL")[0]["saldo"] == 60
    baixar(id_pagar, 60.01, 409)
    segunda = baixar(id_pagar, 60)
    assert detalhe(id_pagar)["titulo"]["situacao"] == "QUITADO"
    baixar(id_pagar, 0.01, 409)
    financeiro(400, "POST", f"/{id_pagar}/baixas/{segunda['id']}/estornar", {"motivo": " "})
    reversao = estornar(id_pagar, segunda["id"])
    assert reversao["usuarioEstornoId"] == 1 and reversao["motivoEstorno"] == "Valor incorreto"
    estornar(id_pagar, segunda["id"], 409)
    historico = detalhe(id_pagar)
    assert [b["id"] for b in historico["baixas"]] == [primeira["id"], segunda["id"]]
    assert historico["titulo"]["valorBaixado"] == Decimal("40.01")

    id_venda = vender()
    venda(200, "POST", f"/{id_venda}/confirmar", {"vencimento": "2000-01-01"})
    receber, = titulos("?tipo=RECEBER&clienteId=1&situacao=VENCIDO")
    assert receber["vendaId"] == id_venda and receber["compraId"] is None
    assert receber["nomeCliente"] == "Z Cliente" and receber["saldo"] == 40
    assert financeiro(200, "GET", "?pagina=2&tamanhoPagina=1")["itens"][0]["id"] == receber["id"]
    assert titulos("?vencimentoFim=2000-01-01") == [receber]
    financeiro(400, "GET", "?vencimentoInicio=2099-01-01&vencimentoFim=2000-01-01")

    id_receber = receber["id"]

    def baixa_concorrente():
        return req_fin("POST", f"/{id_receber}/baixas", {"valor": 30, "formaPagamento": "BOLETO"})

    assert concorrer(baixa_concorrente, baixa_concorrente) == [201, 409]
    antes = saldo()
    venda(409, "POST", f"/{id_venda}/cancelar")
    assert venda(200, "GET", f"/{id_venda}")["pedido"]["situacao"] == "CONFIRMADO" and saldo() == antes
    assert detalhe(id_receber)["titulo"]["situacao"] == "VENCIDO"
    assert indicadores()["contasAReceber"]["saldoVencido"] == 10
    assert indicadores()["contasAPagar"]["saldo"] == 60
    baixa, = detalhe(id_receber)["baixas"]
    estornar(id_receber, baixa["id"])
    venda(200, "POST", f"/{id_venda}/cancelar")
    assert saldo() == antes + 1 and detalhe(id_receber)["titulo"]["situacao"] == "CANCELADO"
    baixar(id_receber, 1, 409)
    estornar(id_pagar, baixa["id"], 404)
    assert indicadores()["contasAReceber"]["totalTitulos"] == 0

    disputa = vender()
    venda(200, "POST", f"/{disputa}/confirmar")
    disputa_titulo, = [x for x in titulos("?tipo=RECEBER") if x["vendaId"] == disputa]
    id_disputa = disputa_titulo["id"]
    assert sql(f"""SELECT count(*) FROM financeiro.titulos WHERE id = {id_disputa}
        AND vencimento = (criado_em AT TIME ZONE 'America/Sao_Paulo')::date
    """, "empresa_b") == "1"
    resultados = concorrer(
        lambda: req_fin("POST", f"/{id_disputa}/baixas", {"valor": 1, "formaPagamento": "CARTAO"}),
        lambda: req_venda("POST", f"/{disputa}/cancelar"),
    )
    assert resultados in ([200, 409], [201, 409])
    assert all(x["compraId"] is None for x in financeiro(200, "GET", jwt=token_a)["itens"])

    # Simula falha de persistência do título depois de alterar estoque e pedido.
    sql("ALTER TABLE financeiro.titulos ADD CHECK (vencimento <> DATE '2035-01-01')", "empresa_b")
    falha_compra, falha_venda = comprar(1), vender(1)
    antes = saldo()
    for api, operacao, id in ((compra, "receber", falha_compra), (venda, "confirmar", falha_venda)):
        api(500, "POST", f"/{id}/{operacao}", {"vencimento": "2035-01-01"})
        assert api(200, "GET", f"/{id}")["pedido"]["situacao"] == "PENDENTE"
    assert saldo() == antes
    assert sql(f"""
        SELECT count(*) FROM estoque.movimentacoes
        WHERE (origem = 'COMPRA' AND referencia_id = {falha_compra})
          OR (origem = 'VENDA' AND referencia_id = {falha_venda})
    """, "empresa_b") == "0"
    assert sql(f"""SELECT count(*) FROM financeiro.titulos
        WHERE compra_id = {falha_compra} OR venda_id = {falha_venda}
    """, "empresa_b") == "0"
    print("Validou geração, filtros, indicadores, baixa parcial/total, estorno, concorrência e rollback financeiro.")
