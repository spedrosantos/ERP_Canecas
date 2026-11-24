import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { authInterceptor } from '../../core/interceptors/auth.interceptor';
import { AuthService } from '../../core/services/auth.service';
import { FEATURES, FeatureService } from '../../core/services/feature.service';
import { Usuarios } from './usuarios';

describe('Usuários', () => {
  it('filtra a lista, envia alterações autenticadas e exibe erros da API', () => {
    const acesso = signal(new Set<string>([FEATURES.usuarios]));
    TestBed.configureTestingModule({
      imports: [Usuarios],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: FeatureService, useValue: { tem: (codigo: string) => acesso().has(codigo) } },
      ],
    });
    const http = TestBed.inject(HttpTestingController);
    TestBed.inject(AuthService).login({ empresaId: 1n, usuarioId: 1n, senha: 'teste' }).subscribe();
    http.expectOne('/api/auth/login').flush({
      empresaId: 1,
      usuarioId: 1,
      nome: 'Operador',
      token: 'jwt-teste',
      expiraEm: new Date(Date.now() + 60_000).toISOString(),
    });

    const fixture = TestBed.createComponent(Usuarios);
    fixture.detectChanges();
    const tela: HTMLElement = fixture.nativeElement;
    const consulta = http.expectOne({ method: 'GET', url: '/api/usuarios' });
    expect(consulta.request.headers.get('Authorization')).toBe('Bearer jwt-teste');
    consulta.flush([
      { id: 23, nome: 'Álvaro', ativo: true, criadoEm: '2026-01-01' },
      { id: 78, nome: 'Beatriz', ativo: false, criadoEm: '2026-01-01' },
    ]);
    fixture.detectChanges();
    expect(tela.querySelector('.cabecalho button')).toBeNull();
    expect(tela.querySelector('.coluna-acoes')).toBeNull();
    acesso.set(new Set([FEATURES.usuarios, FEATURES.cadastrarUsuario, FEATURES.editarUsuario]));
    fixture.detectChanges();

    function preencher(seletor: string, valor: string): void {
      const campo = tela.querySelector<HTMLInputElement>(seletor)!;
      campo.value = valor;
      campo.dispatchEvent(new Event('input'));
      fixture.detectChanges();
    }

    preencher('[type="search"]', 'alvaro');
    expect(tela.querySelector('tbody')?.textContent).toContain('Álvaro');
    expect(tela.querySelector('tbody')?.textContent).not.toContain('Beatriz');
    preencher('[type="search"]', '78');
    expect(tela.querySelector('tbody')?.textContent).toContain('Beatriz');
    preencher('[type="search"]', '');

    const situacao = tela.querySelector('select')!;
    situacao.value = 'ativos';
    situacao.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(tela.querySelectorAll('tbody tr')).toHaveLength(1);

    for (const dialogo of tela.querySelectorAll('dialog')) {
      dialogo.showModal = () => dialogo.setAttribute('open', '');
      dialogo.close = () => dialogo.removeAttribute('open');
    }

    tela.querySelector<HTMLButtonElement>('tbody button')!.click();
    preencher('[formControlName="nome"]', 'Álvaro Silva');
    const salvar = tela.querySelector<HTMLButtonElement>('form button[type="submit"]')!;
    expect(salvar.disabled).toBe(false);
    preencher('#usuario-confirmar-senha', 'senha123');
    expect(salvar.disabled).toBe(true);
    preencher('#usuario-senha', 'senha456');
    expect(salvar.disabled).toBe(true);
    preencher('#usuario-senha', 'curta');
    preencher('#usuario-confirmar-senha', 'curta');
    expect(salvar.disabled).toBe(true);
    preencher('#usuario-senha', '');
    preencher('#usuario-confirmar-senha', '');
    expect(salvar.disabled).toBe(false);
    tela.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    const edicao = http.expectOne({ method: 'PUT', url: '/api/usuarios/23' });
    expect(edicao.request.headers.get('Authorization')).toBe('Bearer jwt-teste');
    expect(edicao.request.body).toEqual({ nome: 'Álvaro Silva' });
    edicao.flush({ id: 23, nome: 'Álvaro Silva', ativo: true, criadoEm: '2026-01-01' });
    fixture.detectChanges();

    tela.querySelector<HTMLButtonElement>('tbody button')!.click();
    const senha = tela.querySelector<HTMLInputElement>('#usuario-senha')!;
    const confirmarSenha = tela.querySelector<HTMLInputElement>('#usuario-confirmar-senha')!;
    tela.querySelector<HTMLButtonElement>('[aria-label="Mostrar senha"]')!.click();
    fixture.detectChanges();
    expect(senha.type).toBe('text');
    expect(confirmarSenha.type).toBe('password');
    tela.querySelector<HTMLButtonElement>('[aria-label="Mostrar confirmação de senha"]')!.click();
    fixture.detectChanges();
    expect(confirmarSenha.type).toBe('text');
    preencher('#usuario-senha', 'senha123');
    preencher('#usuario-confirmar-senha', 'senha123');
    expect(salvar.disabled).toBe(false);
    tela.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    const trocaSenha = http.expectOne({ method: 'PUT', url: '/api/usuarios/23' });
    expect(trocaSenha.request.body).toEqual({ nome: 'Álvaro Silva', senha: 'senha123' });
    trocaSenha.flush({ id: 23, nome: 'Álvaro Silva', ativo: true, criadoEm: '2026-01-01' });
    fixture.detectChanges();

    tela.querySelector<HTMLButtonElement>('tbody button:nth-child(2)')!.click();
    const confirmacao = tela.querySelector<HTMLDialogElement>('dialog[open]')!;
    confirmacao.querySelector<HTMLButtonElement>('.primario')!.click();
    const desativacao = http.expectOne({ method: 'PATCH', url: '/api/usuarios/23/situacao' });
    expect(desativacao.request.headers.get('Authorization')).toBe('Bearer jwt-teste');
    expect(desativacao.request.body).toEqual({ ativo: false });
    desativacao.flush({ mensagem: 'Conflito de situação.' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(confirmacao.querySelector('[role="alert"]')?.textContent).toContain('Conflito de situação.');
    confirmacao.querySelector<HTMLButtonElement>('.acoes-modal button')!.click();

    tela.querySelector<HTMLButtonElement>('.cabecalho button')!.click();
    preencher('[formControlName="nome"]', 'Carla');
    preencher('[formControlName="senha"]', 'senha123');
    tela.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    const cadastro = http.expectOne({ method: 'POST', url: '/api/usuarios' });
    expect(cadastro.request.body).toEqual({ nome: 'Carla', senha: 'senha123' });
    cadastro.flush(null, { status: 403, statusText: 'Forbidden' });
    fixture.detectChanges();
    expect(tela.querySelector('dialog[open] [role="alert"]')?.textContent).toContain('não tem permissão');
    http.verify();
  });
});
