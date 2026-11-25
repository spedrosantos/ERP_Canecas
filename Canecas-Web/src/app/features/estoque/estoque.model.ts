export type TipoMovimentacao = 'ENTRADA' | 'SAIDA';

export type OrigemMovimentacao =
  'COMPRA' | 'VENDA' | 'AJUSTE' | 'ESTORNO';

export interface SaldoEstoque {
  produtoId: number;
  nomeProduto: string;
  unidadeMedida: string;
  quantidade: number;
  estoqueMinimo: number;
  custoMedio: number;
  atualizadoEm: string | null;
}

export interface MovimentacaoEstoque {
  id: number;
  produtoId: number;
  tipo: TipoMovimentacao;
  quantidade: number;
  custoUnitario: number;
  origem: OrigemMovimentacao;
  referenciaId: number | null;
  movimentacaoOriginalId: number | null;
  usuarioId: number;
  justificativa: string | null;
  criadoEm: string;
}

export interface FiltroSaldos {
  busca?: string;
  abaixoDoMinimo?: boolean;
  pagina: number;
  tamanhoPagina: number;
}

export interface FiltroMovimentacoes {
  produtoId?: number;
  tipo?: TipoMovimentacao;
  origem?: OrigemMovimentacao;
  dataInicio?: string;
  dataFim?: string;
  pagina: number;
  tamanhoPagina: number;
}

export interface AjustarEstoque {
  produtoId: number;
  tipo: TipoMovimentacao;
  quantidade: number;
  custoUnitario: number | null;
  justificativa: string;
}

export interface ResultadoPaginado<T> {
  itens: T[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}
