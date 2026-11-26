import { Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, map, of, Subscription, switchMap, timer } from 'rxjs';
import { CadastroAtivo, ComprasService } from './compras.service';

@Component({
  selector: 'app-busca-cadastro',
  template: `
    <label [for]="tipo() + '-busca'">{{ rotulo() }}</label>
    <div class="campo">
      <svg aria-hidden="true"><use href="icons/bootstrap-icons.svg#search" /></svg>
      <input #campoBusca [id]="tipo() + '-busca'" type="text" maxlength="150" autocomplete="off"
        role="combobox" aria-autocomplete="list" aria-haspopup="listbox"
        [attr.aria-expanded]="aberto()" [attr.aria-controls]="aberto() ? tipo() + '-sugestoes' : null"
        [attr.aria-activedescendant]="indice() >= 0 ? tipo() + '-opcao-' + indice() : null"
        [value]="aberto() ? termo() : selecionado()"
        (focus)="buscar('')" (click)="reabrir()" (blur)="fechar()"
        (input)="buscar(campoBusca.value)" (keydown)="teclado($event)" />
      @if (aberto()) {
        <div class="sugestoes">
          <ul [id]="tipo() + '-sugestoes'" role="listbox" [attr.aria-label]="rotulo()">
            @for (item of itens(); track item.id; let i = $index) {
              <li role="option" [id]="tipo() + '-opcao-' + i" [attr.aria-selected]="indice() === i"
                (pointerdown)="$event.preventDefault()" (click)="selecionar(item)">
                <span>{{ item.nome }}</span><small>#{{ item.id }} @if (item.unidadeMedida) { · {{ item.unidadeMedida }} }</small>
              </li>
            }
          </ul>
          @if (carregando()) {
            <p role="status">Buscando…</p>
          } @else if (erro()) {
            <p role="status">Não foi possível carregar as sugestões.</p>
          } @else if (!itens().length) {
            <p role="status">Nenhum resultado.</p>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    :host { display: block; min-width: 0; }
    label { display: block; margin-bottom: .375rem; font-size: .875rem; font-weight: 600; }
    .campo { position: relative; }
    svg { position: absolute; inset: .8rem auto auto .8rem; width: 1rem; height: 1rem; color: var(--secundario); pointer-events: none; }
    input {
      box-sizing: border-box; width: 100%; min-height: 2.625rem; padding: .5rem .75rem .5rem 2.25rem;
      border: 1px solid var(--linha); border-radius: .5rem; background: white; color: inherit; font: inherit;
    }
    input:focus { outline: 2px solid var(--botao); outline-offset: 1px; }
    .sugestoes {
      position: absolute; z-index: 2; inset: calc(100% + .35rem) 0 auto;
      border: 1px solid var(--linha); border-radius: .625rem; background: white;
      box-shadow: 0 .5rem 1.5rem #14203320; overflow: hidden;
    }
    ul { list-style: none; margin: 0; padding: 0; max-height: 15rem; overflow-y: auto; }
    li { display: flex; align-items: baseline; justify-content: space-between; gap: .75rem;
      padding: .625rem .75rem; cursor: pointer; overflow-wrap: anywhere; }
    li:hover, li[aria-selected='true'] { background: var(--superficie); }
    small, p { color: var(--secundario); font-size: .875rem; }
    p { margin: 0; padding: .625rem .75rem; }
  `]
})
export class BuscaCadastro {
  private readonly api = inject(ComprasService);
  private readonly destroyRef = inject(DestroyRef);
  private requisicao?: Subscription;

  readonly tipo = input.required<'fornecedores' | 'produtos'>();
  readonly rotulo = input.required<string>();
  readonly selecionado = input('');
  readonly escolhido = output<CadastroAtivo>();

  protected readonly termo = signal('');
  protected readonly aberto = signal(false);
  protected readonly itens = signal<CadastroAtivo[]>([]);
  protected readonly indice = signal(-1);
  protected readonly carregando = signal(false);
  protected readonly erro = signal(false);

  protected buscar(termo: string) {
    this.requisicao?.unsubscribe();
    this.termo.set(termo);
    this.aberto.set(true);
    this.itens.set([]);
    this.indice.set(-1);
    this.carregando.set(true);
    this.erro.set(false);
    const busca = termo.trim();
    this.requisicao = timer(busca ? 250 : 0).pipe(
      switchMap(() => this.api.listarCadastros(this.tipo(), busca, 1)),
      map(dados => dados.itens),
      catchError(() => of(null)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(itens => {
      this.itens.set(itens ?? []);
      this.erro.set(itens === null);
      this.carregando.set(false);
    });
  }

  protected reabrir() {
    if (!this.aberto()) this.buscar('');
  }

  protected fechar() {
    this.requisicao?.unsubscribe();
    this.indice.set(-1);
    this.aberto.set(false);
  }

  protected selecionar(item: CadastroAtivo) {
    this.fechar();
    this.escolhido.emit(item);
  }

  protected teclado(evento: KeyboardEvent) {
    if (evento.key === 'Escape' && this.aberto()) {
      evento.preventDefault();
      this.fechar();
    } else if (evento.key === 'Enter') {
      evento.preventDefault();
      const item = this.itens()[this.indice()];
      if (item) this.selecionar(item);
    } else if (evento.key === 'ArrowDown' || evento.key === 'ArrowUp') {
      evento.preventDefault();
      if (!this.aberto()) {
        this.buscar('');
        return;
      }
      const total = this.itens().length;
      if (!total) return;
      const anterior = this.indice();
      const proximo = evento.key === 'ArrowDown'
        ? (anterior + 1) % total
        : (anterior <= 0 ? total - 1 : anterior - 1);
      this.indice.set(proximo);
      const campo = evento.currentTarget as HTMLInputElement;
      campo.ownerDocument.getElementById(`${this.tipo()}-opcao-${proximo}`)?.scrollIntoView?.({ block: 'nearest' });
    }
  }
}
