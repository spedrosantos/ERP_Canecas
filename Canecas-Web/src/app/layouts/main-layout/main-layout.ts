import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { TemaService } from '../../core/services/tema.service';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
})
export class MainLayout {
  protected readonly auth = inject(AuthService);
  private readonly temaService = inject(TemaService);

  protected readonly tema = this.temaService.temaAtual;
  protected readonly empresa = computed(() => {
    const sessao = this.auth.usuario();
    if (sessao && this.temaService.status() !== 'aplicado') {
      return `Empresa ${sessao.empresaId}`;
    }

    return this.tema().nomeFantasia;
  });
  protected readonly recolhida = signal(false);
  protected readonly menuMovelAberto = signal(false);
  protected readonly logoComErro = signal<string | null>(null);

  protected expandirConfiguracoes(evento: MouseEvent, menu: HTMLDetailsElement): void {
    if (this.recolhida()) {
      this.recolhida.set(false);
      if (menu.open) {
        evento.preventDefault();
      }
    }
  }
}
