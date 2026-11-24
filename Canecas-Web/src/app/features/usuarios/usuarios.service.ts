import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { CadastrarUsuario, EditarUsuario, Usuario } from './usuario';

@Injectable({ providedIn: 'root' })
export class UsuariosService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/usuarios';

  listar() {
    return this.http.get<Usuario[]>(this.url);
  }

  cadastrar(dados: CadastrarUsuario) {
    return this.http.post<Usuario>(this.url, dados);
  }

  editar(id: number, dados: EditarUsuario) {
    return this.http.put<Usuario>(`${this.url}/${id}`, dados);
  }

  alterarSenha(id: number, senha: string) {
    return this.http.patch<Usuario>(`${this.url}/${id}/senha`, { senha });
  }

  alterarSituacao(id: number, ativo: boolean) {
    return this.http.patch<Usuario>(`${this.url}/${id}/situacao`, { ativo });
  }
}
