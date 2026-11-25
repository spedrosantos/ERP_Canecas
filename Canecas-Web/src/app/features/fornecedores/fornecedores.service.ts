import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Fornecedor, FornecedoresPaginados, DadosFornecedor, EmpresaConsulta, EnderecoConsulta, FiltroFornecedores } from './fornecedor';

@Injectable({ providedIn: 'root' })
export class FornecedoresService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/fornecedores';

  listar(filtro: FiltroFornecedores) {
    let params = new HttpParams({ fromObject: {
      pagina: filtro.pagina,
      tamanhoPagina: filtro.tamanhoPagina,
    } });
    if (filtro.busca) {
      params = params.set('busca', filtro.busca);
    }
    if (filtro.tipoPessoa) {
      params = params.set('tipoPessoa', filtro.tipoPessoa);
    }
    if (filtro.ativo !== undefined) {
      params = params.set('ativo', filtro.ativo);
    }

    return this.http.get<FornecedoresPaginados>(this.url, { params });
  }

  cadastrar(dados: DadosFornecedor) {
    return this.http.post<Fornecedor>(this.url, dados);
  }

  editar(id: number, dados: DadosFornecedor) {
    return this.http.put<Fornecedor>(`${this.url}/${id}`, dados);
  }

  alterarSituacao(id: number, ativo: boolean) {
    return this.http.patch<Fornecedor>(`${this.url}/${id}/situacao`, { ativo });
  }

  consultarCep(cep: string) {
    return this.http.get<EnderecoConsulta>('/api/consultas/cep', { params: { cep } });
  }

  consultarCnpj(cnpj: string) {
    return this.http.get<EmpresaConsulta>('/api/consultas/cnpj', { params: { cnpj } });
  }
}
