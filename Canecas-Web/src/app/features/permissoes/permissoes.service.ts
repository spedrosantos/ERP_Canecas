import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export interface UsuarioPermissoes {
  id: number;
  nome: string;
  ativo: boolean;
  proprioUsuario: boolean;
  featureIds: number[];
}

export interface Feature {
  id: number;
  idFeaturePai: number | null;
  codigo: string;
  nome: string;
  ativo: boolean;
}

export interface PermissoesEmLote {
  usuarios: UsuarioPermissoes[];
  features: Feature[];
  featureIdsGerenciaveis: number[];
}

export interface AtualizarPermissoesEmLote {
  usuarioIds: number[];
  concederIds: number[];
  revogarIds: number[];
}

@Injectable({ providedIn: 'root' })
export class PermissoesService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/usuarios/permissoes/lote';

  consultar() {
    return this.http.get<PermissoesEmLote>(this.url);
  }

  atualizar(operacao: AtualizarPermissoesEmLote) {
    return this.http.put<void>(this.url, operacao);
  }
}
