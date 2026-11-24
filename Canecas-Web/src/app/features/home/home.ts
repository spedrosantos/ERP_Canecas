import { Component, inject } from '@angular/core';

import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-home',
  standalone: true,
  template: `
    <main>
      <h1>Olá, {{ auth.usuario()?.nome }}</h1>
      <button type="button" (click)="auth.logout()">Sair</button>
    </main>
  `,
  styles: `
    main {
      padding: clamp(1.5rem, 5vw, 4rem);
    }
  `,
})
export class Home {
  protected readonly auth = inject(AuthService);
}
