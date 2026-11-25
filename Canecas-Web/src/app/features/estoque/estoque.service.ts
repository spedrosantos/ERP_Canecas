import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import {
  SaldoEstoque,
  MovimentacaoEstoque,
  FiltroSaldos,
  FiltroMovimentacoes,
  AjustarEstoque,
  ResultadoPaginado
} from './estoque.model';

@Injectable({ providedIn: 'root' })
export class EstoqueService {
  private readonly http = inject(HttpClient);
  private readonly api = '/api/estoque';

  listarSaldos(filtro: FiltroSaldos) {
    let params = new HttpParams()
      .set('pagina', filtro.pagina)
      .set('tamanhoPagina', filtro.tamanhoPagina);

    if (filtro.busca)
      params = params.set('busca', filtro.busca);

    if (filtro.abaixoDoMinimo !== undefined)
      params = params.set(
        'abaixoDoMinimo', filtro.abaixoDoMinimo
      );

    return this.http.get<ResultadoPaginado<SaldoEstoque>>(
      `${this.api}/saldos`, { params }
    );
  }

  listarMovimentacoes(filtro: FiltroMovimentacoes) {
    let params = new HttpParams()
      .set('pagina', filtro.pagina)
      .set('tamanhoPagina', filtro.tamanhoPagina);

    if (filtro.produtoId)
      params = params.set('produtoId', filtro.produtoId);

    if (filtro.tipo)
      params = params.set('tipo', filtro.tipo);

    if (filtro.origem)
      params = params.set('origem', filtro.origem);

    if (filtro.dataInicio)
      params = params.set('dataInicio', filtro.dataInicio);

    if (filtro.dataFim)
      params = params.set('dataFim', filtro.dataFim);

    return this.http.get<ResultadoPaginado<MovimentacaoEstoque>>(
      `${this.api}/movimentacoes`, { params }
    );
  }

  ajustar(dto: AjustarEstoque) {
    return this.http.post<MovimentacaoEstoque>(
      `${this.api}/movimentacoes/ajuste`, dto
    );
  }

  configurarMinimo(produtoId: number, estoqueMinimo: number) {
    return this.http.patch<void>(
      `${this.api}/saldos/${produtoId}/minimo`,
      { estoqueMinimo }
    );
  }

  estornar(id: number, justificativa: string) {
    return this.http.post<MovimentacaoEstoque>(
      `${this.api}/movimentacoes/${id}/estorno`,
      { justificativa }
    );
  }
}
