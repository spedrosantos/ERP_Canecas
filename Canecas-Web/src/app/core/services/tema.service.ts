import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of, startWith, Subject, switchMap, tap } from 'rxjs';

import { EmpresaTema } from '../models/empresa-tema';

const TEMA_PADRAO: Readonly<EmpresaTema> = {
  nomeFantasia: 'Canecas ERP',
  corPrimaria: '#1F2937',
  corSecundaria: '#4B5563',
  corFundo: '#F9FAFB',
  fonteTitulo: 'Inter',
  fonteTexto: 'Inter',
  logoUrl: null,
};

const FONTES_SUPORTADAS = new Map([
  ['inter', 'Inter'],
  ['fredoka', 'Fredoka'],
  ['nunito', 'Nunito'],
]);

@Injectable({ providedIn: 'root' })
export class TemaService {
  private readonly http = inject(HttpClient);
  private readonly documento = inject(DOCUMENT);
  private readonly empresaSelecionada = new Subject<number | string | null>();

  readonly temaAtual = toSignal(
    this.empresaSelecionada.pipe(
      switchMap((empresaId) => {
        if (empresaId === null) {
          return of(TEMA_PADRAO);
        }

        return this.http
          .get<EmpresaTema>(`/api/empresas/${encodeURIComponent(empresaId)}/tema`)
          .pipe(startWith(TEMA_PADRAO), catchError(() => of(TEMA_PADRAO)));
      }),
      tap((tema) => this.aplicarTema(tema)),
    ),
    { initialValue: TEMA_PADRAO },
  );

  carregarTema(empresaId: number | string): void {
    this.empresaSelecionada.next(empresaId);
  }

  restaurarTemaPadrao(): void {
    this.empresaSelecionada.next(null);
  }

  private aplicarTema(tema: Readonly<EmpresaTema>): void {
    const estilo = this.documento.documentElement.style;

    estilo.setProperty('--cor-primaria', tema.corPrimaria);
    estilo.setProperty('--cor-secundaria', tema.corSecundaria);
    estilo.setProperty('--cor-fundo', tema.corFundo);
    estilo.setProperty('--fonte-titulo', this.resolverFonte(tema.fonteTitulo));
    estilo.setProperty('--fonte-texto', this.resolverFonte(tema.fonteTexto));
  }

  private resolverFonte(fonte: string): string {
    const familia = FONTES_SUPORTADAS.get(fonte.trim().toLowerCase()) ?? TEMA_PADRAO.fonteTexto;

    return `'${familia}', system-ui, sans-serif`;
  }
}
