import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  Produto,
  SalvarProduto,
  FiltroProdutos,
  ProdutosPaginados,
  CadastroAuxiliar,
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

  listarCategorias(somenteAtivas = true) {
    return this.listarCadastro<Categoria>('categorias', somenteAtivas);
  }

  listarUnidades(somenteAtivas = true) {
    return this.listarCadastro<UnidadeMedida>('unidades-medida', somenteAtivas);
  }

  private listarCadastro<T>(cadastro: CadastroAuxiliar, somenteAtivas: boolean) {
    const params = somenteAtivas ? { ativo: 'true' } : undefined;
    return this.http.get<T[]>(`/api/${cadastro}`, { params });
  }

  salvarCadastro(cadastro: CadastroAuxiliar, id: number | null, descricao: string, sigla: string) {
    const dados = cadastro === 'categorias' ? { nome: descricao } : { descricao, sigla };
    const url = `/api/${cadastro}`;

    return id === null
      ? this.http.post(url, dados)
      : this.http.put(`${url}/${id}`, dados);
  }

  alterarSituacaoCadastro(cadastro: CadastroAuxiliar, id: number, ativo: boolean) {
    return this.http.patch<void>(`/api/${cadastro}/${id}/situacao`, { ativo });
  }
}
