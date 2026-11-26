import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export type SituacaoPedido = 'PENDENTE' | 'CONFIRMADO' | 'CANCELADO';

export interface Pedido {
  id: number;
  clienteId: number;
  usuarioId: number;
  situacao: SituacaoPedido;
  valorTotal: number;
  observacao: string | null;
  criadoEm: string;
  atualizadoEm: string | null;
  confirmadoEm: string | null;
  canceladoEm: string | null;
}

export interface ItemVenda {
  produtoId: number;
  quantidade: number;
  precoUnitario: number;
}

export interface PedidoDetalhe {
  pedido: Pedido;
  nomeCliente: string | null;
  itens: (ItemVenda & {
    id: number;
    custoUnitario: number | null;
    nomeProduto: string | null;
    unidadeMedida: string | null;
  })[];
}

export interface DisponibilidadeProduto {
  produtoId: number;
  nomeProduto: string;
  unidadeMedida: string;
  quantidadeDisponivel: number;
}

export interface SalvarPedido {
  clienteId: number;
  observacao: string;
  itens: ItemVenda[];
}

export interface Pagina<T> {
  itens: T[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}

export const URL_VENDAS = '/api/vendas/pedidos';

@Injectable({ providedIn: 'root' })
export class VendasService {
  private readonly http = inject(HttpClient);
  private readonly url = URL_VENDAS;

  listar(params: Record<string, string | number>) {
    return this.http.get<Pagina<Pedido>>(this.url, { params });
  }

  obter(id: number) {
    return this.http.get<PedidoDetalhe>(`${this.url}/${id}`);
  }

  consultarDisponibilidade(produtoId: number) {
    return this.http.get<DisponibilidadeProduto>(`${this.url}/produtos/${produtoId}/disponibilidade`);
  }

  salvar(dto: SalvarPedido, id?: number) {
    return id === undefined
      ? this.http.post<PedidoDetalhe>(this.url, dto)
      : this.http.put<PedidoDetalhe>(`${this.url}/${id}`, dto);
  }

  alterarSituacao(id: number, acao: 'confirmar' | 'cancelar') {
    return this.http.post<PedidoDetalhe>(`${this.url}/${id}/${acao}`, null);
  }
}
