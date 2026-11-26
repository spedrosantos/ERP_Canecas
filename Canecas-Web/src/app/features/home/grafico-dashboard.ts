import { Component, DestroyRef, ElementRef, afterNextRender, effect, inject, input, signal } from '@angular/core';
import type { EChartsType } from 'echarts/core';
import type { OpcoesGrafico } from './dashboard-graficos';

@Component({
  selector: 'app-grafico-dashboard',
  template: '<div class="superficie" role="img" [attr.aria-label]="descricao()"></div>'
    + '@if (falhou()) { <p role="status">Não foi possível renderizar o gráfico.</p> }',
  styles: [
    ':host { display: block; min-width: 0; }',
    '.superficie { height: var(--altura-grafico, 17rem); width: 100%; }',
    'p { margin: 0; padding: .75rem; }',
  ],
})
export class GraficoDashboard {
  readonly opcoes = input.required<OpcoesGrafico>();
  readonly descricao = input.required<string>();
  protected readonly falhou = signal(false);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly instancia = signal<EChartsType | null>(null);

  constructor() {
    afterNextRender(() => void this.iniciar());
    effect(() => this.instancia()?.setOption(this.opcoes(), { notMerge: true }));
  }

  private async iniciar(): Promise<void> {
    const elemento = this.host.nativeElement.querySelector<HTMLElement>('.superficie');
    if (!elemento || !elemento.clientWidth) return;

    try {
      const { iniciarGrafico } = await import('./dashboard-echarts');
      if (this.destroyRef.destroyed) return;
      const grafico = iniciarGrafico(elemento);
      const observador = new ResizeObserver(() => grafico.resize());
      observador.observe(elemento);
      this.destroyRef.onDestroy(() => {
        observador.disconnect();
        grafico.dispose();
      });
      this.instancia.set(grafico);
    } catch {
      if (!this.destroyRef.destroyed) this.falhou.set(true);
    }
  }
}
