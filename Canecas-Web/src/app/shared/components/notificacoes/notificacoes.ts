import { Component, ElementRef, effect, inject, viewChild } from '@angular/core';
import { NotificacoesService } from '../../../core/services/notificacoes.service';

@Component({
  selector: 'app-notificacoes',
  templateUrl: './notificacoes.html',
  styleUrl: './notificacoes.scss',
})
export class Notificacoes {
  protected readonly notificacoes = inject(NotificacoesService);
  private readonly painel = viewChild<ElementRef<HTMLElement>>('painel');

  constructor() {
    effect(() => {
      const itens = this.notificacoes.itens();
      const painel = this.painel()?.nativeElement;
      if (!painel) return;
      if (itens.length) {
        // Reabre no topo para exibir erros mesmo com um diálogo modal aberto.
        painel.hidePopover?.();
        painel.showPopover?.();
      } else {
        painel.hidePopover?.();
      }
    });
  }
}
