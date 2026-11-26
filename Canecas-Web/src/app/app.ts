import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Notificacoes } from './shared/components/notificacoes/notificacoes';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Notificacoes],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('frontend');
}
