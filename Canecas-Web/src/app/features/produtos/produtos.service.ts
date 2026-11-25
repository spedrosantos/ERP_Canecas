import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  Produto,
  SalvarProduto,
  FiltroProdutos,
  ProdutosPaginados,
  Categoria,
  UnidadeMedida
} from './produto';

@Injectable({ providedIn: 'root' })
export class ProdutosService {
  private readonly http = inject(HttpClient);
  private readonly api = '/api/produtos';

  listar(filtro: FiltroProdutos) {
    let params = new HttpParams()
      .set('pagina', filtro.pagina)
      .set('tamanhoPagina', filtro.tamanhoPagina);

    if (filtro.busca)
      params = params.set('busca', filtro.busca);

    if (filtro.categoriaId)
      params = params.set('categoriaId', filtro.categoriaId);

    if (filtro.tipo)
      params = params.set('tipo', filtro.tipo);

    if (filtro.ativo !== undefined)
      params = params.set('ativo', filtro.ativo);

    return this.http.get<ProdutosPaginados>(
      this.api, { params }
    );
  }

  obter(id: number) {
    return this.http.get<Produto>(`${this.api}/${id}`);
  }

  cadastrar(produto: SalvarProduto) {
    return this.http.post<Produto>(this.api, produto);
  }

  atualizar(id: number, produto: SalvarProduto) {
    return this.http.put<Produto>(
      `${this.api}/${id}`, produto
    );
  }

  alterarSituacao(id: number, ativo: boolean) {
    return this.http.patch<void>(
      `${this.api}/${id}/situacao`, { ativo }
    );
  }

  listarCategorias() {
    return this.http.get<Categoria[]>(
      '/api/categorias?ativo=true'
    );
  }

  listarUnidades() {
    return this.http.get<UnidadeMedida[]>(
      '/api/unidades-medida?ativo=true'
    );
  }
}