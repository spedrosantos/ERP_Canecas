import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { LoginRequest } from '../models/login-request';
import { LoginResponse } from '../models/login-response';
import { TemaService } from './tema.service';

const CHAVE_SESSAO = 'canecas-erp:sessao';
const LIMITE_INATIVIDADE = 30 * 60_000;
const EVENTOS_ATIVIDADE = ['pointerdown', 'keydown', 'wheel'];

interface SessaoPersistida {
  usuario: LoginResponse;
  codigoEmpresa: string;
  ultimaInteracao: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly temaService = inject(TemaService);
  private readonly documento = inject(DOCUMENT);
  private readonly sessao = signal<SessaoPersistida | null>(null);
  private readonly expirou = signal(false);
  private temporizador?: ReturnType<typeof setTimeout>;

  readonly usuario = computed(() => this.sessao()?.usuario ?? null);
  readonly sessaoExpirada = this.expirou.asReadonly();

  constructor() {
    this.restaurarSessao();
    for (const evento of EVENTOS_ATIVIDADE) {
      this.documento.addEventListener(evento, this.registrarInteracao, { capture: true, passive: true });
    }

    inject(DestroyRef).onDestroy(() => {
      clearTimeout(this.temporizador);
      for (const evento of EVENTOS_ATIVIDADE) {
        this.documento.removeEventListener(evento, this.registrarInteracao, true);
      }
    });
  }

  login({ empresaId, usuarioId, senha }: LoginRequest): Observable<LoginResponse> {
    this.expirou.set(false);
    // Preserva os códigos long no JSON, inclusive acima de Number.MAX_SAFE_INTEGER.
    const corpo = `{"empresaId":${empresaId},"usuarioId":${usuarioId},"senha":${JSON.stringify(senha)}}`;

    return this.http
      .post<LoginResponse>('/api/auth/login', corpo, {
        headers: { 'Content-Type': 'application/json' },
        timeout: 15000,
      })
      .pipe(tap((resposta) => this.iniciarSessao(resposta, empresaId.toString())));
  }

  obterToken(): string | null {
    const sessao = this.sessao();
    return sessao && this.calcularPrazo(sessao) > Date.now() ? sessao.usuario.token : null;
  }

  invalidarToken(token: string): void {
    if (this.usuario()?.token === token) {
      this.encerrarSessao(true);
    }
  }

  logout(): void {
    this.encerrarSessao(false);
  }

  private iniciarSessao(usuario: LoginResponse, codigoEmpresa: string): void {
    const sessao = { usuario, codigoEmpresa, ultimaInteracao: Date.now() };
    if (!usuario.token || !(this.calcularPrazo(sessao) > Date.now())) {
      throw new Error('A API retornou uma sessão inválida ou expirada.');
    }

    this.sessao.set(sessao);
    this.persistirSessao(sessao);
    this.agendarExpiracao();
  }

  private restaurarSessao(): void {
    try {
      const armazenada = this.documento.defaultView?.sessionStorage.getItem(CHAVE_SESSAO);
      if (!armazenada) {
        return;
      }

      const sessao: SessaoPersistida | null = JSON.parse(armazenada);
      const agora = Date.now();
      if (
        !sessao ||
        typeof sessao.usuario?.token !== 'string' ||
        !sessao.usuario.token ||
        typeof sessao.usuario.expiraEm !== 'string' ||
        typeof sessao.codigoEmpresa !== 'string' ||
        !/^\d{1,19}$/.test(sessao.codigoEmpresa) ||
        !Number.isFinite(sessao.ultimaInteracao) ||
        sessao.ultimaInteracao > agora ||
        !(this.calcularPrazo(sessao) > agora)
      ) {
        this.persistirSessao(null);
        this.expirou.set(true);
        return;
      }

      this.sessao.set(sessao);
      this.agendarExpiracao();
      this.temaService.carregarTema(sessao.codigoEmpresa);
    } catch {
      this.persistirSessao(null);
    }
  }

  private persistirSessao(sessao: SessaoPersistida | null): void {
    try {
      const armazenamento = this.documento.defaultView?.sessionStorage;
      if (sessao) {
        armazenamento?.setItem(CHAVE_SESSAO, JSON.stringify(sessao));
      } else {
        armazenamento?.removeItem(CHAVE_SESSAO);
      }
    } catch {
      // A sessão continua disponível em memória quando o navegador bloqueia o armazenamento.
    }
  }

  private readonly registrarInteracao = (evento: Event): void => {
    const sessao = this.sessao();
    if (!sessao) {
      return;
    }

    const agora = Date.now();
    if (this.calcularPrazo(sessao) <= agora) {
      this.encerrarSessao(true);
      return;
    }

    if (evento.isTrusted && agora > sessao.ultimaInteracao) {
      const atualizada = { ...sessao, ultimaInteracao: agora };
      this.sessao.set(atualizada);
      this.persistirSessao(atualizada);
    }
  };

  private calcularPrazo(sessao: SessaoPersistida): number {
    return Math.min(
      Date.parse(sessao.usuario.expiraEm),
      sessao.ultimaInteracao + LIMITE_INATIVIDADE,
    );
  }

  private agendarExpiracao(): void {
    clearTimeout(this.temporizador);
    const sessao = this.sessao();
    if (!sessao) {
      return;
    }

    const intervalo = this.calcularPrazo(sessao) - Date.now();
    if (intervalo <= 0) {
      this.encerrarSessao(true);
      return;
    }

    this.temporizador = setTimeout(() => this.agendarExpiracao(), intervalo);
  }

  private encerrarSessao(expirada: boolean): void {
    clearTimeout(this.temporizador);
    this.sessao.set(null);
    this.persistirSessao(null);
    this.expirou.set(expirada);
    this.temaService.restaurarTemaPadrao();
    void this.router.navigateByUrl('/login', { replaceUrl: true });
  }
}
