import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Cliente, ClientesPaginados, DadosCliente, EmpresaConsulta, EnderecoConsulta, FiltroClientes } from './cliente';

@Injectable({ providedIn: 'root' })
export class ClientesService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/clientes';

  listar(filtro: FiltroClientes) {
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

    return this.http.get<ClientesPaginados>(this.url, { params });
  }

  cadastrar(dados: DadosCliente) {
    return this.http.post<Cliente>(this.url, dados);
  }

  editar(id: number, dados: DadosCliente) {
    return this.http.put<Cliente>(`${this.url}/${id}`, dados);
  }

  alterarSituacao(id: number, ativo: boolean) {
    return this.http.patch<Cliente>(`${this.url}/${id}/situacao`, { ativo });
  }

  consultarCep(cep: string) {
    return this.http.get<EnderecoConsulta>('/api/consultas/cep', { params: { cep } });
  }

  consultarCnpj(cnpj: string) {
    return this.http.get<EmpresaConsulta>('/api/consultas/cnpj', { params: { cnpj } });
  }
}
