import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { computed, effect, inject, Injectable, signal, untracked } from '@angular/core';
import { catchError, exhaustMap, finalize, fromEvent, merge, Observable, of, shareReplay, tap, timer } from 'rxjs';

import { AuthService } from './auth.service';

export const FEATURES = {
  usuarios: 'USUARIO',
  cadastrarUsuario: 'USUARIO_CADASTRAR',
  editarUsuario: 'USUARIO_EDITAR',
  alterarSenhaUsuario: 'USUARIO_ALTERAR_SENHA',
  permissoes: 'USUARIO_PERMISSOES',
  clientes: 'CLIENTE',
  fornecedores: 'FORNECEDOR',
  produtos: 'PRODUTO',
  estoque: 'ESTOQUE',
  compras: 'COMPRA',
} as const;

interface FeatureEfetiva {
  id: number;
  idFeaturePai: number | null;
  codigo: string;
}

@Injectable({ providedIn: 'root' })
export class FeatureService {
  private readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);
  private readonly janela = inject(DOCUMENT).defaultView;
  private readonly estado = signal<{ token: string; features: FeatureEfetiva[] } | null>(null);
  private pendente?: { token: string; consulta: Observable<FeatureEfetiva[]> };
  private readonly atuais = computed(() => {
    const estado = this.estado();
    return estado?.token === this.auth.usuario()?.token ? estado?.features : undefined;
  });

  readonly prontas = computed(() => this.atuais() !== undefined);

  constructor() {
    effect((cleanup) => {
      if (!this.auth.usuario()?.token || !this.janela) {
        this.estado.set(null);
        return;
      }

      const atualizacoes = merge(timer(0, 30_000), fromEvent(this.janela, 'focus'))
        .pipe(exhaustMap(() => untracked(() => this.atualizar())))
        .subscribe();
      cleanup(() => atualizacoes.unsubscribe());
    });
  }

  tem(codigo: string): boolean {
    return this.atuais()?.some(feature => feature.codigo === codigo) ?? false;
  }

  permiteRota(codigo: string): boolean {
    return this.tem(codigo) && (codigo !== FEATURES.usuarios ||
      this.tem(FEATURES.cadastrarUsuario) || this.tem(FEATURES.editarUsuario) ||
      this.tem(FEATURES.alterarSenhaUsuario));
  }

  temFilha(codigo: string): boolean {
    const features = this.atuais();
    const pai = features?.find(feature => feature.codigo === codigo);
    return !!pai && !!features?.some(feature => feature.idFeaturePai === pai.id);
  }

  atualizar(): Observable<FeatureEfetiva[]> {
    const token = this.auth.obterToken();
    if (!token) {
      return of([]);
    }

    if (this.pendente?.token === token) {
      return this.pendente.consulta;
    }

    const consulta = this.http.get<FeatureEfetiva[]>('/api/usuarios/me/features', { timeout: 15_000 })
      .pipe(
        catchError(() => of<FeatureEfetiva[]>([])),
        tap(features => {
          if (this.auth.obterToken() === token) {
            this.estado.set({ token, features: Array.isArray(features) ? features : [] });
          }
        }),
        finalize(() => {
          if (this.pendente?.consulta === consulta) {
            this.pendente = undefined;
          }
        }),
        shareReplay({ bufferSize: 1, refCount: true }),
      );
    this.pendente = { token, consulta };
    return consulta;
  }
}
