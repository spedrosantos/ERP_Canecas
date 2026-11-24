import { HttpClient } from '@angular/common/http';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { LoginRequest } from '../models/login-request';
import { LoginResponse } from '../models/login-response';
import { TemaService } from './tema.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly temaService = inject(TemaService);
  private readonly sessao = signal<LoginResponse | null>(null);
  private readonly expirou = signal(false);
  private temporizador?: ReturnType<typeof setTimeout>;

  readonly usuario = this.sessao.asReadonly();
  readonly sessaoExpirada = this.expirou.asReadonly();

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.temporizador));
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
      .pipe(tap((resposta) => this.iniciarSessao(resposta)));
  }

  obterToken(): string | null {
    const sessao = this.sessao();
    return sessao && Date.parse(sessao.expiraEm) > Date.now() ? sessao.token : null;
  }

  invalidarToken(token: string): void {
    if (this.sessao()?.token === token) {
      this.encerrarSessao(true);
    }
  }

  logout(): void {
    this.encerrarSessao(false);
  }

  private iniciarSessao(resposta: LoginResponse): void {
    const expiraEm = Date.parse(resposta.expiraEm);
    if (!resposta.token || !Number.isFinite(expiraEm) || expiraEm <= Date.now()) {
      throw new Error('A API retornou uma sessão inválida ou expirada.');
    }

    clearTimeout(this.temporizador);
    this.sessao.set(resposta);
    this.agendarExpiracao(resposta.token, expiraEm);
  }

  private agendarExpiracao(token: string, expiraEm: number): void {
    const intervalo = expiraEm - Date.now();
    if (intervalo <= 0) {
      this.invalidarToken(token);
      return;
    }

    this.temporizador = setTimeout(
      () => this.agendarExpiracao(token, expiraEm),
      Math.min(intervalo, 2_147_483_647),
    );
  }

  private encerrarSessao(expirada: boolean): void {
    clearTimeout(this.temporizador);
    this.sessao.set(null);
    this.expirou.set(expirada);
    this.temaService.restaurarTemaPadrao();
    void this.router.navigateByUrl('/login', { replaceUrl: true });
  }
}
