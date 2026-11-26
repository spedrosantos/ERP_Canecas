"""Executa Vendas, Compras e Financeiro contra um PostgreSQL descartável. Requer Linux, PostgreSQL e .NET 10.

Uso na raiz: dotnet build && python3 tests/vendas/integracao.py
"""
import base64
import hashlib
import hmac
import http.client
import json
import os
from concurrent.futures import ThreadPoolExecutor
from contextlib import ExitStack
from decimal import Decimal
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import secrets
import socket
import subprocess
import tempfile
import threading
import time
from urllib.error import HTTPError
from urllib.request import Request, urlopen

from financeiro import executar_financeiro
from dashboard import executar_dashboard

RAIZ = Path(__file__).resolve().parents[2]
ROTA = "/api/vendas/pedidos"
CHAVE = secrets.token_hex(32)


def token(empresa, usuario=1):
    def codificar(valor):
        return base64.urlsafe_b64encode(valor).decode().rstrip("=")

    cabecalho = codificar(b'{"alg":"HS256","typ":"JWT"}')
    dados = codificar(json.dumps({
        "iss": "CanecasERP", "aud": "CanecasERP", "exp": int(time.time()) + 300,
        "empresa_id": str(empresa), "usuario_id": str(usuario),
    }).encode())
    assinatura = hmac.digest(CHAVE.encode(), f"{cabecalho}.{dados}".encode(), hashlib.sha256)
    return f"{cabecalho}.{dados}.{codificar(assinatura)}"


class IdentidadeDeTeste(BaseHTTPRequestHandler):
    def do_GET(self):
        esperados = {"/api/usuarios/me/features/verificar?codigo=" + codigo
                     for codigo in ("VENDA", "COMPRA", "FINANCEIRO", "ESTOQUE")}
        autorizacao = self.headers.get("Authorization")
        venda_apenas = autorizacao == f"Bearer {TOKEN_SO_VENDAS}" and self.path.endswith("?codigo=VENDA")
        status = 204 if (self.path in esperados and autorizacao in TOKENS) or venda_apenas else 403
        self.send_response(status)
        self.end_headers()

    def log_message(self, formato, *args):
        pass


TOKEN_A, TOKEN_B, TOKEN_A_OPERADOR = token(1), token(2), token(1, 2)
TOKEN_SO_VENDAS = token(2, 2)
TOKENS = {f"Bearer {credencial}" for credencial in (TOKEN_A, TOKEN_B, TOKEN_A_OPERADOR)}


def cliente_http(porta, rota, token_padrao):
    def requisitar(metodo, sufixo="", dados=None, jwt=token_padrao, caminho=rota):
        cabecalhos = {"Content-Type": "application/json"}
        if jwt:
            cabecalhos["Authorization"] = f"Bearer {jwt}"
        corpo = json.dumps(dados).encode() if dados is not None else None
        requisicao = Request(f"http://127.0.0.1:{porta}{caminho}{sufixo}", corpo, cabecalhos, method=metodo)
        try:
            resposta = urlopen(requisicao, timeout=12)
        except HTTPError as erro:
            resposta = erro
        with resposta:
            conteudo = resposta.read()
            tipo = resposta.headers.get("Content-Type", "")
            return resposta.status, json.loads(conteudo, parse_float=Decimal) if conteudo and "json" in tipo else None

    def verificar(status, metodo, sufixo="", dados=None, jwt=token_padrao, caminho=rota):
        codigo, resposta = requisitar(metodo, sufixo, dados, jwt, caminho)
        assert codigo == status, (metodo, sufixo, status, codigo, resposta)
        return resposta

    return requisitar, verificar


def executar(porta, sql):
    requisitar, verificar = cliente_http(porta, ROTA, TOKEN_A)

    def item(produto, quantidade, preco=1):
        return {"produtoId": produto, "quantidade": quantidade, "precoUnitario": preco}

    def salvar(*itens, cliente=1, status=201):
        return verificar(status, "POST", dados={
            "clienteId": cliente, "observacao": " teste ", "valorTotal": 9000, "itens": itens,
        })

    def criar(*itens):
        return salvar(*itens)["pedido"]["id"]

    def saldo(produto):
        return verificar(200, "GET", f"/produtos/{produto}/disponibilidade")["quantidadeDisponivel"]

    def disputa(*acoes, jwt=TOKEN_A):
        barreira = threading.Barrier(len(acoes))

        def chamar(sufixo):
            barreira.wait(timeout=5)
            return requisitar("POST", sufixo, jwt=jwt)[0]

        with ThreadPoolExecutor(max_workers=len(acoes)) as tarefas:
            return sorted(tarefas.map(chamar, acoes))

    verificar(401, "GET", jwt=None)
    verificar(403, "GET", jwt=token(1, 99))
    verificar(404, "GET", jwt=None, caminho="/api/estoque/saldos")
    assert [c["id"] for c in verificar(200, "GET", "/clientes")["itens"]] == [1, 2]
    assert [p["id"] for p in verificar(200, "GET", "/produtos?busca=2")["itens"]] == [2]
    assert saldo(3) == 0
    verificar(404, "GET", "/produtos/4/disponibilidade")

    for itens in ([], [item(1, 1), item(1, 1)], [item(1, 0)], [item(1, 0.0001)],
                  [item(1, 1, 0)], [item(1, 1, 1.00001)], [item(4, 1)],
                  [item(1, 999999999999.999, 99999999999.9999)]):
        salvar(*itens, status=400)
    salvar(item(1, 1), cliente=3, status=400)
    verificar(400, "GET", "?dataInicio=2026-11-27&dataFim=2026-11-26")

    pendente = salvar(item(1, 1, 1.005), item(2, 1, 1.005))
    id_pendente = pendente["pedido"]["id"]
    assert pendente["pedido"]["valorTotal"] == Decimal("2.01")
    assert pendente["pedido"]["observacao"] == "teste"
    assert all(i["custoUnitario"] is None for i in pendente["itens"])
    editado = verificar(200, "PUT", f"/{id_pendente}", {"clienteId": 2, "itens": [item(1, 2, 3), item(3, 1, 4)]})
    assert editado["pedido"]["valorTotal"] == 10 and editado["nomeCliente"] == "A Cliente"
    assert [i["produtoId"] for i in editado["itens"]] == [1, 3]
    assert verificar(200, "GET", "?clienteId=2&situacao=PENDENTE&tamanhoPagina=1")["total"] == 1
    assert verificar(200, "GET", "?clienteId=2&situacao=PENDENTE&pagina=2&tamanhoPagina=1")["itens"] == []
    verificar(404, "GET", f"/{id_pendente}", jwt=TOKEN_B)
    cancelado = verificar(200, "POST", f"/{id_pendente}/cancelar")
    assert cancelado["pedido"]["canceladoEm"] and cancelado["pedido"]["confirmadoEm"] is None
    assert not sql("SELECT id FROM estoque.movimentacoes") and saldo(1) == 8
    verificar(409, "POST", f"/{id_pendente}/cancelar")
    verificar(409, "PUT", f"/{id_pendente}", {"clienteId": 2, "itens": [item(1, 2)]})

    insuficiente = criar(item(1, 1), item(2, 3), item(3, 1))
    verificar(400, "POST", f"/{insuficiente}/confirmar")
    assert saldo(1) == 8 and saldo(2) == 2
    assert not sql("SELECT id FROM estoque.movimentacoes")
    assert verificar(200, "GET", f"/{insuficiente}")["pedido"]["situacao"] == "PENDENTE"

    venda = criar(item(1, 3, 20), item(2, 1, 12))
    assert disputa(f"/{venda}/confirmar", f"/{venda}/confirmar") == [200, 409]
    confirmada = verificar(200, "GET", f"/{venda}")
    assert confirmada["pedido"]["confirmadoEm"] and confirmada["pedido"]["valorTotal"] == 72
    assert [i["custoUnitario"] for i in confirmada["itens"]] == [8, 3]
    assert saldo(1) == 5 and saldo(2) == 1
    verificar(409, "PUT", f"/{venda}", {"clienteId": 1, "itens": [item(1, 2)]})
    assert sql(f"SELECT count(*) FROM estoque.movimentacoes WHERE origem = 'VENDA' AND tipo = 'SAIDA' AND referencia_id = {venda}") == "2"

    sql("UPDATE estoque.saldos SET quantidade = 999999999999.999 WHERE produto_id = 2")
    verificar(400, "POST", f"/{venda}/cancelar")
    assert saldo(1) == 5 and not sql("SELECT id FROM estoque.movimentacoes WHERE origem = 'ESTORNO'")
    assert sql(f"SELECT count(*) FROM financeiro.titulos WHERE venda_id = {venda} AND cancelado_em IS NULL") == "1"
    assert verificar(200, "GET", f"/{venda}")["pedido"]["situacao"] == "CONFIRMADO"

    sql("""
        UPDATE estoque.saldos SET quantidade = 1 WHERE produto_id = 2;
        UPDATE estoque.saldos SET custo_medio = 10 WHERE produto_id = 1;
        UPDATE cadastro.produtos SET ativo = false WHERE id = 2;
    """)
    assert disputa(f"/{venda}/cancelar", f"/{venda}/cancelar", jwt=TOKEN_A_OPERADOR) == [200, 409]
    finalizado = verificar(200, "GET", f"/{venda}")
    assert finalizado["pedido"]["canceladoEm"] and finalizado["pedido"]["confirmadoEm"]
    assert saldo(1) == 8 and sql("SELECT custo_medio FROM estoque.saldos WHERE produto_id = 1") == "9.2500"
    assert sql(f"""
        SELECT count(*) FROM estoque.movimentacoes e
        JOIN estoque.movimentacoes v ON v.id = e.movimentacao_original_id
        WHERE v.referencia_id = {venda} AND v.origem = 'VENDA' AND e.origem = 'ESTORNO'
          AND e.tipo = 'ENTRADA' AND e.usuario_id = 2
          AND e.custo_unitario = v.custo_unitario AND e.quantidade = v.quantidade
    """) == "2"

    concorrente_a, concorrente_b = criar(item(1, 6)), criar(item(1, 6))
    assert disputa(f"/{concorrente_a}/confirmar", f"/{concorrente_b}/confirmar") == [200, 400]
    assert saldo(1) == 2
    assert verificar(200, "GET", jwt=TOKEN_B)["total"] == 0
    assert verificar(200, "GET", "/produtos/1/disponibilidade", jwt=TOKEN_B)["quantidadeDisponivel"] == 8
    verificar(404, "POST", f"/{venda}/confirmar", jwt=TOKEN_B)
    print("Validou JWT, VENDA, isolamento, cadastros, filtros, totais, edição, estoque, concorrência e rollback.")


def iniciar():
    pg = Path(subprocess.check_output(["pg_config", "--bindir"], text=True).strip())
    projetos = ("MS-Venda", "MS-Compra", "MS-Financeiro", "MS-Estoque")
    dlls = {projeto: RAIZ / projeto / "bin/Debug/net10.0" / (projeto + ".dll")
            for projeto in projetos}
    if not all(dll.is_file() for dll in dlls.values()):
        raise SystemExit("Execute dotnet build antes do teste.")

    with tempfile.TemporaryDirectory(prefix="erp-vendas-") as pasta, \
            ThreadingHTTPServer(("127.0.0.1", 0), IdentidadeDeTeste) as identidade:
        banco = str(Path(pasta) / "pg")
        comando_pg = [str(pg / "pg_ctl"), "-D", banco]
        # PostgreSQL aceita conexões apenas pelo socket dentro do diretório temporário.
        subprocess.run([str(pg / "initdb"), "-D", banco, "-U", "erp_teste", "-A", "trust",
                        "--no-locale"], check=True, stdout=subprocess.DEVNULL)
        try:
            subprocess.run([*comando_pg, "-l", str(Path(pasta) / "postgres.log"),
                            "-o", f"-k {pasta} -h ''", "-w", "start"], check=True,
                           stdout=subprocess.DEVNULL)

            def psql(sql, database="empresa_a"):
                return subprocess.check_output(
                    [str(pg / "psql"), "-XAt", "-h", pasta, "-p", "5432", "-U", "erp_teste",
                     "-d", database, "-v", "ON_ERROR_STOP=1", "-c", sql], text=True,
                ).strip()

            for empresa in ("empresa_a", "empresa_b"):
                psql(f"CREATE DATABASE {empresa}", "postgres")
                psql(Path(__file__).with_name("esquema.sql").read_text(), empresa)
            psql("""
                CREATE TABLE empresas (id bigint PRIMARY KEY, nome_banco text NOT NULL, ativo boolean NOT NULL);
                INSERT INTO empresas VALUES (1, 'empresa_a', true), (2, 'empresa_b', true);
            """, "postgres")

            threading.Thread(target=identidade.serve_forever, daemon=True).start()
            conexao = f"Host={pasta};Port=5432;Username=erp_teste;Database="
            ambiente = {
                **os.environ, "ASPNETCORE_ENVIRONMENT": "Development", "DOTNET_ENVIRONMENT": "Development",
                "Jwt__Key": CHAVE, "Jwt__Issuer": "CanecasERP", "Jwt__Audience": "CanecasERP",
                "ConnectionStrings__MasterConnection": conexao + "postgres",
                "ConnectionStrings__empresa_a": conexao + "empresa_a",
                "ConnectionStrings__empresa_b": conexao + "empresa_b",
                "Integracoes__Identidade__BaseUrl": f"http://127.0.0.1:{identidade.server_port}",
            }
            with ExitStack() as recursos:
                recursos.callback(identidade.shutdown)
                portas = {}
                for projeto, dll in dlls.items():
                    portas[projeto] = iniciar_api(pasta, projeto, dll, ambiente, recursos)

                try:
                    executar(portas["MS-Venda"], psql)
                    executar_financeiro(portas, psql, cliente_http, TOKEN_A, TOKEN_B)
                    executar_dashboard(portas, psql, cliente_http, TOKEN_A, TOKEN_B, TOKEN_SO_VENDAS)
                except Exception:
                    for projeto in projetos:
                        print((Path(pasta) / (projeto + ".log")).read_text())
                    raise
        finally:
            subprocess.run([*comando_pg, "-m", "immediate", "stop"],
                           stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=False)


def iniciar_api(pasta, projeto, dll, ambiente, recursos):
    with socket.socket() as reserva:
        reserva.bind(("127.0.0.1", 0))
        porta = reserva.getsockname()[1]

    log = recursos.enter_context((Path(pasta) / (projeto + ".log")).open("w"))
    api = recursos.enter_context(subprocess.Popen(
        ["dotnet", str(dll), "--urls", f"http://127.0.0.1:{porta}"],
        cwd=RAIZ / projeto, env=ambiente, stdout=log, stderr=subprocess.STDOUT,
    ))

    def parar():
        if api.poll() is None:
            api.terminate()
            try:
                api.wait(timeout=10)
            except subprocess.TimeoutExpired:
                api.kill()

    recursos.callback(parar)
    prazo = time.monotonic() + 15
    while time.monotonic() < prazo and api.poll() is None:
        try:
            with urlopen(f"http://127.0.0.1:{porta}/openapi/v1.json", timeout=1):
                return porta
        except (OSError, http.client.HTTPException):
            time.sleep(0.1)

    raise RuntimeError(projeto + " não iniciou. Verifique a compilação e a configuração de teste.")


if __name__ == "__main__":
    iniciar()
