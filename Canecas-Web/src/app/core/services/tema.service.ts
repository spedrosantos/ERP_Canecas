import { DOCUMENT } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of, startWith, Subject, switchMap, tap } from 'rxjs';

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

type EstadoTema = {
  tema: Readonly<EmpresaTema>;
  status: 'padrao' | 'carregando' | 'aplicado' | 'empresa-invalida' | 'erro';
};

const ESTADO_PADRAO: EstadoTema = { tema: TEMA_PADRAO, status: 'padrao' };
const LOGOS_LOCAIS = new Map([['1', 'logos/boracup.jpeg']]);

@Injectable({ providedIn: 'root' })
export class TemaService {
  private readonly http = inject(HttpClient);
  private readonly documento = inject(DOCUMENT);
  private readonly empresaSelecionada = new Subject<number | string | null>();

  private readonly estado = toSignal(
    this.empresaSelecionada.pipe(
      switchMap((empresaId) => {
        if (empresaId === null) {
          return of(ESTADO_PADRAO);
        }

        return this.http
          .get<EmpresaTema>(`/api/empresas/${encodeURIComponent(empresaId)}/tema`)
          .pipe(
            map((tema): EstadoTema => ({
              tema: {
                ...tema,
                logoUrl: tema.logoUrl?.trim() || LOGOS_LOCAIS.get(String(empresaId)) || null,
              },
              status: 'aplicado',
            })),
            startWith({ tema: TEMA_PADRAO, status: 'carregando' } satisfies EstadoTema),
            catchError((erro: unknown) =>
              of<EstadoTema>({
                tema: TEMA_PADRAO,
                status: erro instanceof HttpErrorResponse && erro.status === 404
                  ? 'empresa-invalida'
                  : 'erro',
              }),
            ),
          );
      }),
      tap(({ tema }) => this.aplicarTema(tema)),
    ),
    { initialValue: ESTADO_PADRAO },
  );

  readonly temaAtual = computed(() => this.estado().tema);
  readonly status = computed(() => this.estado().status);

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
