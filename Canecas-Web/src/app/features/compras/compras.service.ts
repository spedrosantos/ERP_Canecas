import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export type SituacaoPedido = 'PENDENTE' | 'RECEBIDO' | 'CANCELADO';

export interface Pedido {
  id: number;
  fornecedorId: number;
  usuarioId: number;
  situacao: SituacaoPedido;
  valorTotal: number;
  observacao: string | null;
  criadoEm: string;
  atualizadoEm: string | null;
  recebidoEm: string | null;
}

export interface ItemCompra {
  produtoId: number;
  quantidade: number;
  custoUnitario: number;
}

export interface PedidoDetalhe {
  pedido: Pedido;
  nomeFornecedor: string | null;
  itens: (ItemCompra & { id: number; nomeProduto?: string | null; unidadeMedida?: string | null })[];
}

export interface SalvarPedido {
  fornecedorId: number;
  observacao: string;
  itens: ItemCompra[];
}

export interface Pagina<T> {
  itens: T[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}

export const URL_COMPRAS = '/api/compras/pedidos';

@Injectable({ providedIn: 'root' })
export class ComprasService {
  private readonly http = inject(HttpClient);
  private readonly url = URL_COMPRAS;

  listar(params: Record<string, string | number>) {
    return this.http.get<Pagina<Pedido>>(this.url, { params });
  }

  obter(id: number) {
    return this.http.get<PedidoDetalhe>(`${this.url}/${id}`);
  }

  salvar(dto: SalvarPedido, id?: number) {
    return id === undefined
      ? this.http.post<PedidoDetalhe>(this.url, dto)
      : this.http.put<PedidoDetalhe>(`${this.url}/${id}`, dto);
  }

  alterarSituacao(id: number, acao: 'receber' | 'cancelar', vencimento?: string) {
    return this.http.post<PedidoDetalhe>(`${this.url}/${id}/${acao}`, acao === 'receber' ? { vencimento } : null);
  }
}
