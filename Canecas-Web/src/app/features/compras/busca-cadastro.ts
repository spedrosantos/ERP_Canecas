import { Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';
import { CadastroAtivo, ComprasService, Pagina } from './compras.service';

@Component({
  selector: 'app-busca-cadastro',
  template: `
    <div class="busca">
      <label>
        {{ rotulo() }}
        <input #termo type="search" maxlength="150" placeholder="Código ou nome"
          (keydown.enter)="$event.preventDefault(); buscar(termo.value)" />
      </label>
      <button type="button" (click)="buscar(termo.value)">Buscar</button>
    </div>
    @if (carregando()) {
      <p role="status">Buscando…</p>
    } @else if (erro()) {
      <p role="alert">Não foi possível consultar o cadastro.</p>
    } @else if (resultado(); as dados) {
      @if (!dados.itens.length) {
        <p>Nenhum resultado encontrado.</p>
      } @else {
        <ul>
          @for (item of dados.itens; track item.id) {
            <li><button type="button" (click)="selecionar(item)">{{ item.nome }} · #{{ item.id }}</button></li>
          }
        </ul>
      }
      @if (dados.total > dados.tamanhoPagina) {
        <div class="busca">
          <button type="button" [disabled]="dados.pagina <= 1" (click)="buscar(busca, dados.pagina - 1)">Anterior</button>
          <span>Página {{ dados.pagina }}</span>
          <button type="button" [disabled]="dados.pagina * dados.tamanhoPagina >= dados.total"
            (click)="buscar(busca, dados.pagina + 1)">Próxima</button>
        </div>
      }
    }
  `,
  styles: [`
    .busca { display: flex; flex-wrap: wrap; align-items: end; gap: .5rem; }
    label { display: grid; gap: .25rem; flex: 1; min-width: min(100%, 14rem); }
    input, button { font: inherit; padding: .5rem; border: 1px solid var(--linha); border-radius: .4rem; }
    button { cursor: pointer; background: white; color: inherit; }
    button:disabled { cursor: default; opacity: .5; }
    ul { list-style: none; display: flex; flex-wrap: wrap; gap: .375rem; padding: 0; }
  `]
})
export class BuscaCadastro {
  private readonly api = inject(ComprasService);
  private readonly destroyRef = inject(DestroyRef);
  private requisicao?: Subscription;

  readonly tipo = input.required<'fornecedores' | 'produtos'>();
  readonly rotulo = input.required<string>();
  readonly escolhido = output<CadastroAtivo>();

  protected readonly resultado = signal<Pagina<CadastroAtivo> | null>(null);
  protected readonly carregando = signal(false);
  protected readonly erro = signal(false);
  protected busca = '';

  protected buscar(termo: string, pagina = 1) {
    this.requisicao?.unsubscribe();
    this.busca = termo.trim();
    this.carregando.set(true);
    this.erro.set(false);
    this.requisicao = this.api.listarCadastros(this.tipo(), this.busca, pagina)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: dados => {
          this.resultado.set(dados);
          this.carregando.set(false);
        },
        error: () => {
          this.erro.set(true);
          this.carregando.set(false);
        }
      });
  }

  protected selecionar(item: CadastroAtivo) {
    this.resultado.set(null);
    this.escolhido.emit(item);
  }
}
