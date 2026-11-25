export type TipoProduto = 'ACABADO' | 'MATERIA_PRIMA' | 'REVENDA';

export interface Categoria {
  id: number;
  nome: string;
  ativo: boolean;
}

export interface UnidadeMedida {
  id: number;
  sigla: string;
  descricao: string;
  ativo: boolean;
}

export interface SalvarProduto {
  categoriaId: number;
  unidadeMedidaId: number;
  nome: string;
  descricao: string | null;
  tipo: TipoProduto;
}

export interface Produto extends SalvarProduto {
  id: number;
  categoria: string;
  unidadeMedida: string;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string | null;
}

export interface FiltroProdutos {
  busca?: string;
  categoriaId?: number;
  tipo?: TipoProduto;
  ativo?: boolean;
  pagina: number;
  tamanhoPagina: number;
}

export interface ProdutosPaginados {
  itens: Produto[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}
