import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged, map, tap } from 'rxjs';

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
  protected readonly envioValidado = signal(false);

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
      this.envioValidado.set(false);
    });
    this.destroyRef.onDestroy(() => this.temaService.restaurarTemaPadrao());
  }

  protected enviar(): void {
    this.formulario.markAllAsTouched();
    this.envioValidado.set(this.formulario.valid && this.estadoTema() === 'aplicado');
  }
}
