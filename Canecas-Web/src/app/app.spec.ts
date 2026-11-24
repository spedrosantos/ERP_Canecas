import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';

import { routes } from './app.routes';
import { App } from './app';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { AuthService } from './core/services/auth.service';
import { TemaService } from './core/services/tema.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter(routes),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('encerra por expiração ou logout e restaura o tema padrão', () => {
    const auth = TestBed.inject(AuthService);
    const tema = TestBed.inject(TemaService);
    const http = TestBed.inject(HttpTestingController);
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    vi.useFakeTimers({ toFake: ['Date', 'setTimeout', 'clearTimeout'] });
    try {
      function iniciarSessao(): void {
        tema.carregarTema(1);
        http.expectOne('/api/empresas/1/tema').flush({
          ...tema.temaAtual(), nomeFantasia: 'Empresa Exemplo',
        });
        auth.login({ empresaId: 1n, usuarioId: 1n, senha: 'teste' }).subscribe();
        http.expectOne('/api/auth/login').flush({
          empresaId: 1, usuarioId: 1, nome: 'Ana', token: 'token-teste',
          expiraEm: new Date(Date.now() + 1000).toISOString(),
        });
        expect(auth.obterToken()).toBe('token-teste');
      }

      iniciarSessao();
      vi.advanceTimersByTime(1000);
      expect(auth.usuario()).toBeNull();
      expect(auth.sessaoExpirada()).toBe(true);
      expect(tema.temaAtual().nomeFantasia).toBe('Canecas ERP');

      iniciarSessao();
      auth.logout();
      vi.advanceTimersByTime(1000);
      expect(auth.obterToken()).toBeNull();
      expect(auth.sessaoExpirada()).toBe(false);
      expect(tema.temaAtual().nomeFantasia).toBe('Canecas ERP');
      expect(navegar).toHaveBeenCalledTimes(2);
      expect(navegar).toHaveBeenLastCalledWith('/login', { replaceUrl: true });
      http.verify();
    } finally {
      vi.useRealTimers();
    }
  });

  it('protege a home, bloqueia login duplicado e encerra a sessão quando o token é recusado', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    const auth = TestBed.inject(AuthService);
    const tema = TestBed.inject(TemaService);
    const http = TestBed.inject(HttpTestingController);
    const client = TestBed.inject(HttpClient);
    const tela: HTMLElement = fixture.nativeElement;
    await router.navigateByUrl('/home');
    await fixture.whenStable();
    expect(router.url).toBe('/login');

    vi.useFakeTimers({ toFake: ['Date', 'setInterval', 'clearInterval'] });
    try {
      for (const [id, valor] of Object.entries({
        empresa: '1',
        usuario: '9223372036854775807',
        senha: 'senha "teste"',
      })) {
        const input = tela.querySelector<HTMLInputElement>(`#${id}`)!;
        input.value = valor;
        input.dispatchEvent(new Event('input'));
      }
      vi.advanceTimersByTime(350);
      const consultaTema = http.expectOne('/api/empresas/1/tema');
      expect(consultaTema.request.headers.has('Authorization')).toBe(false);
      consultaTema.flush({ ...tema.temaAtual(), nomeFantasia: 'Empresa Exemplo' });

      const form = tela.querySelector('form')!;
      const enviar = () => form.dispatchEvent(new Event('submit', { cancelable: true }));
      enviar();
      enviar();
      const login = http.expectOne('/api/auth/login');
      expect(login.request.method).toBe('POST');
      expect(login.request.headers.has('Authorization')).toBe(false);
      expect(login.request.body).toBe(
        '{"empresaId":1,"usuarioId":9223372036854775807,"senha":' + JSON.stringify('senha "teste"') + '}',
      );
      login.flush(null, { status: 401, statusText: 'Unauthorized' });
      fixture.detectChanges();
      expect(tela.querySelector('[role="alert"]')?.textContent).toContain('Credenciais inválidas');

      enviar();
      http.expectOne('/api/auth/login').flush(null, { status: 503, statusText: 'Unavailable' });
      fixture.detectChanges();
      expect(tela.querySelector('[role="alert"]')?.textContent).toContain('Não foi possível entrar');

      enviar();
      http.expectOne('/api/auth/login').flush({
        empresaId: 1, usuarioId: 1, nome: 'Ana', token: 'token-teste',
        expiraEm: new Date(Date.now() + 60_000).toISOString(),
      });
      await fixture.whenStable();
      expect(router.url).toBe('/home');
      expect(tela.querySelector('h1')?.textContent).toContain('Ana');
      expect(tema.temaAtual().nomeFantasia).toBe('Empresa Exemplo');
      expect(tela.querySelector('aside header .marca-nome')?.textContent).toBe('Empresa Exemplo');
      expect(tela.querySelector('aside .usuario-nome')?.textContent).toBe('Ana');
      expect(tela.querySelector('aside .sair')?.textContent).toContain('Sair');
      expect(tela.querySelector('nav a')?.getAttribute('aria-current')).toBe('page');

      const controleDesktop = tela.querySelector<HTMLButtonElement>('aside .controle-desktop')!;
      controleDesktop.click();
      fixture.detectChanges();
      expect(controleDesktop.getAttribute('aria-expanded')).toBe('false');
      expect(tela.querySelector('.estrutura')?.classList.contains('recolhida')).toBe(true);

      const controleMovel = tela.querySelector<HTMLButtonElement>('aside .controle-movel')!;
      controleMovel.click();
      fixture.detectChanges();
      expect(controleMovel.getAttribute('aria-expanded')).toBe('true');
      expect(tela.querySelector('aside')?.classList.contains('menu-movel-aberto')).toBe(true);

      client.get('/api/empresas/1/tema').subscribe();
      const temaPublico = http.expectOne('/api/empresas/1/tema');
      expect(temaPublico.request.headers.has('Authorization')).toBe(false);
      temaPublico.flush({});

      client.post('https://exemplo.invalid/api/usuarios', {}).subscribe();
      const externo = http.expectOne('https://exemplo.invalid/api/usuarios');
      expect(externo.request.headers.has('Authorization')).toBe(false);
      externo.flush({});

      client.post('/api/usuarios', {}).subscribe({ error: () => undefined });
      const protegido = http.expectOne('/api/usuarios');
      expect(protegido.request.headers.get('Authorization')).toBe('Bearer token-teste');
      protegido.flush(null, { status: 401, statusText: 'Unauthorized' });
      await fixture.whenStable();
      expect(router.url).toBe('/login');
      expect(auth.obterToken()).toBeNull();
      expect(tema.temaAtual().nomeFantasia).toBe('Canecas ERP');
      expect(tela.textContent).toContain('Sua sessão expirou');
      http.verify();
    } finally {
      vi.useRealTimers();
    }
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
