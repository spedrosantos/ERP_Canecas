CREATE SCHEMA cadastro;
CREATE SCHEMA identidade;
CREATE SCHEMA vendas;
CREATE SCHEMA estoque;
CREATE TABLE cadastro.clientes (id bigint PRIMARY KEY, nome text NOT NULL, ativo boolean NOT NULL);
CREATE TABLE cadastro.unidades_medida (id bigint PRIMARY KEY, sigla text NOT NULL);
CREATE TABLE cadastro.produtos (
    id bigint PRIMARY KEY, nome text NOT NULL, ativo boolean NOT NULL,
    unidade_medida_id bigint NOT NULL REFERENCES cadastro.unidades_medida
);
CREATE TABLE identidade.usuarios (id bigint PRIMARY KEY);
CREATE TABLE vendas.pedidos (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cliente_id bigint NOT NULL REFERENCES cadastro.clientes,
    usuario_id bigint NOT NULL REFERENCES identidade.usuarios,
    situacao varchar(20) NOT NULL CHECK (situacao IN ('PENDENTE', 'CONFIRMADO', 'CANCELADO')),
    valor_total numeric(19,2) NOT NULL,
    observacao varchar(500),
    criado_em timestamptz NOT NULL,
    atualizado_em timestamptz,
    confirmado_em timestamptz,
    cancelado_em timestamptz
);
CREATE TABLE vendas.pedidos_itens (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    pedido_id bigint NOT NULL REFERENCES vendas.pedidos,
    produto_id bigint NOT NULL REFERENCES cadastro.produtos,
    quantidade numeric(15,3) NOT NULL CHECK (quantidade > 0),
    preco_unitario numeric(15,4) NOT NULL CHECK (preco_unitario > 0),
    custo_unitario numeric(15,4),
    UNIQUE (pedido_id, produto_id)
);
CREATE TABLE estoque.saldos (
    produto_id bigint PRIMARY KEY REFERENCES cadastro.produtos,
    quantidade numeric(15,3) NOT NULL DEFAULT 0 CHECK (quantidade >= 0),
    estoque_minimo numeric(15,3) NOT NULL DEFAULT 0,
    custo_medio numeric(15,4) NOT NULL DEFAULT 0,
    atualizado_em timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE estoque.movimentacoes (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    produto_id bigint NOT NULL REFERENCES cadastro.produtos,
    tipo varchar(10) NOT NULL,
    quantidade numeric(15,3) NOT NULL,
    custo_unitario numeric(15,4) NOT NULL,
    origem varchar(20) NOT NULL,
    referencia_id bigint,
    movimentacao_original_id bigint UNIQUE REFERENCES estoque.movimentacoes,
    usuario_id bigint NOT NULL REFERENCES identidade.usuarios,
    justificativa varchar(500),
    criado_em timestamptz NOT NULL,
    CHECK (
        (origem = 'VENDA' AND referencia_id IS NOT NULL AND movimentacao_original_id IS NULL)
        OR (origem = 'ESTORNO' AND referencia_id IS NULL AND movimentacao_original_id IS NOT NULL)
    )
);
INSERT INTO cadastro.clientes VALUES (1, 'Z Cliente', true), (2, 'A Cliente', true), (3, 'Inativo', false);
INSERT INTO cadastro.unidades_medida VALUES (1, 'UN');
INSERT INTO cadastro.produtos VALUES
    (1, 'Z Caneca', true, 1), (2, 'A Caneca', true, 1), (3, 'Sem saldo', true, 1), (4, 'Inativo', false, 1);
INSERT INTO identidade.usuarios VALUES (1), (2);
INSERT INTO estoque.saldos (produto_id, quantidade, custo_medio) VALUES (1, 8, 8), (2, 2, 3);
