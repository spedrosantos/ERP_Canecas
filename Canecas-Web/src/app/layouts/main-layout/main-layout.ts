import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { FEATURES, FeatureService } from '../../core/services/feature.service';
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
  private readonly features = inject(FeatureService);
  private readonly router = inject(Router);
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
  protected readonly mostrarUsuarios = computed(() =>
    this.features.permiteRota(FEATURES.usuarios) && this.features.temFilha(FEATURES.usuarios),
  );
  protected readonly mostrarPermissoes = computed(() => this.features.tem(FEATURES.permissoes));

  constructor() {
    effect(() => {
      if (this.router.currentNavigation() || !this.auth.obterToken() || !this.features.prontas()) {
        return;
      }

      let rota = this.router.routerState.snapshot.root;
      while (rota.firstChild) {
        rota = rota.firstChild;
      }

      const codigo: unknown = rota.data['feature'];
      if (typeof codigo === 'string' && !this.features.permiteRota(codigo)) {
        untracked(() => void this.router.navigateByUrl('/home', { replaceUrl: true }));
      }
    });
  }

  protected expandirConfiguracoes(evento: MouseEvent, menu: HTMLDetailsElement): void {
    if (this.recolhida()) {
      this.recolhida.set(false);
      if (menu.open) {
        evento.preventDefault();
      }
    }
  }
}
