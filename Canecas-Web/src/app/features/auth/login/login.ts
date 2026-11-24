import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, map, tap } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { TemaService } from '../../../core/services/tema.service';

function normalizarCodigo(valor: string): string | null {
  const codigo = valor.trim().replace(/^0+/, '');

  if (!/^\d{1,19}$/.test(codigo) || BigInt(codigo) > 9223372036854775807n) {
    return null;
  }

  return codigo;
}

const validarCodigo: ValidatorFn = ({ value }) => {
  if (typeof value !== 'string' || !value.trim()) {
    return { required: true };
  }

  return normalizarCodigo(value) === null ? { codigo: true } : null;
};

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly temaService = inject(TemaService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly formulario = inject(FormBuilder).nonNullable.group({
    empresa: ['', validarCodigo],
    usuario: ['', validarCodigo],
    senha: ['', Validators.required],
  });
  protected readonly controles = this.formulario.controls;
  protected readonly tema = this.temaService.temaAtual;
  protected readonly estadoTema = this.temaService.status;
  protected readonly senhaVisivel = signal(false);
  protected readonly logoComErro = signal<string | null>(null);
  protected readonly enviando = signal(false);
  protected readonly erroLogin = signal('');
  protected readonly sessaoExpirada = this.auth.sessaoExpirada;

  constructor() {
    this.controles.empresa.valueChanges
      .pipe(
        map(normalizarCodigo),
        distinctUntilChanged(),
        tap(() => this.temaService.restaurarTemaPadrao()),
        debounceTime(350),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((codigo) => {
        if (codigo !== null) {
          this.temaService.carregarTema(codigo);
        }
      });

    this.formulario.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.erroLogin.set('');
    });
    this.destroyRef.onDestroy(() => {
      if (!this.auth.obterToken()) {
        this.temaService.restaurarTemaPadrao();
      } else if (this.estadoTema() === 'padrao') {
        const empresaId = normalizarCodigo(this.controles.empresa.value);
        if (empresaId !== null) {
          this.temaService.carregarTema(empresaId);
        }
      }
    });
  }

  protected enviar(): void {
    if (this.enviando()) {
      return;
    }

    this.formulario.markAllAsTouched();
    const { empresa, usuario, senha } = this.formulario.getRawValue();
    const empresaId = normalizarCodigo(empresa);
    const usuarioId = normalizarCodigo(usuario);
    if (this.formulario.invalid || empresaId === null || usuarioId === null) {
      return;
    }

    this.erroLogin.set('');
    this.enviando.set(true);
    this.auth
      .login({ empresaId: BigInt(empresaId), usuarioId: BigInt(usuarioId), senha })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.enviando.set(false)),
      )
      .subscribe({
        next: () => void this.router.navigateByUrl('/home', { replaceUrl: true }),
        error: (erro: unknown) => {
          this.erroLogin.set(
            erro instanceof HttpErrorResponse && erro.status === 401
              ? 'Credenciais inválidas. Confira os dados informados.'
              : 'Não foi possível entrar agora. Tente novamente em instantes.',
          );
        },
      });
  }
}
