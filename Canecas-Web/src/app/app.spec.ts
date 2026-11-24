import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';

import { routes } from './app.routes';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('atualiza o tema ao digitar e cancela consultas de códigos anteriores', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/login');
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    const empresa = compiled.querySelector<HTMLInputElement>('#empresa')!;
    const marca = compiled.querySelector('.marca-nome')!;
    const http = TestBed.inject(HttpTestingController);

    function digitar(codigo: string): void {
      empresa.value = codigo;
      empresa.dispatchEvent(new Event('input'));
    }

    vi.useFakeTimers({ toFake: ['Date', 'setInterval', 'clearInterval'] });
    try {
      expect(marca.textContent).toContain('Canecas ERP');

      digitar('12');
      vi.advanceTimersByTime(349);
      http.expectNone('/api/empresas/12/tema');
      vi.advanceTimersByTime(1);
      const consultaAnterior = http.expectOne('/api/empresas/12/tema');
      fixture.detectChanges();
      expect(compiled.querySelector('#estado-empresa')?.textContent).toContain('Consultando');

      digitar('13');
      expect(consultaAnterior.cancelled).toBe(true);
      vi.advanceTimersByTime(350);
      http.expectOne('/api/empresas/13/tema').flush({
        nomeFantasia: 'Empresa Exemplo',
        corPrimaria: '#123456',
        corSecundaria: '#654321',
        corFundo: '#ffffff',
        fonteTitulo: 'Fredoka',
        fonteTexto: 'Nunito',
        logoUrl: '/logo.png',
      });
      fixture.detectChanges();
      expect(marca.textContent).toContain('Empresa Exemplo');
      expect(compiled.querySelector('.marca-logo')?.getAttribute('src')).toBe('/logo.png');

      digitar('013');
      vi.advanceTimersByTime(350);
      http.expectNone('/api/empresas/13/tema');

      digitar('');
      fixture.detectChanges();
      expect(marca.textContent).toContain('Canecas ERP');
      expect(compiled.querySelector('.marca-logo')).toBeNull();

      const senha = compiled.querySelector<HTMLInputElement>('#senha')!;
      const alternarSenha = compiled.querySelector<HTMLButtonElement>('.alternar-senha')!;
      expect(senha.type).toBe('password');
      alternarSenha.click();
      fixture.detectChanges();
      expect(senha.type).toBe('text');
      http.verify();
    } finally {
      vi.useRealTimers();
    }
  });
});
