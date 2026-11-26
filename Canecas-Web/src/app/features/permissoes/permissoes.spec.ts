import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { authInterceptor } from '../../core/interceptors/auth.interceptor';
import { AuthService } from '../../core/services/auth.service';
import { FEATURES, FeatureService } from '../../core/services/feature.service';
import { Permissoes } from './permissoes';

describe('Permissões em lote', () => {
  it('mostra os usuários e salva marcações e desmarcações em uma única operação', () => {
    TestBed.configureTestingModule({
      imports: [Permissoes],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: FeatureService,
          useValue: { tem: (codigo: string) => [
            FEATURES.usuarios, FEATURES.editarUsuario, FEATURES.permissoes,
          ].some(feature => feature === codigo) },
        },
      ],
    });
    const http = TestBed.inject(HttpTestingController);
    const auth = TestBed.inject(AuthService);
    auth.login({ empresaId: 1n, usuarioId: 1n, senha: 'teste' }).subscribe();
    http.expectOne('/api/auth/login').flush({
      empresaId: 1, usuarioId: 1, nome: 'Operador', token: 'jwt-teste',
      expiraEm: new Date(Date.now() + 60_000).toISOString(),
    });

    const fixture = TestBed.createComponent(Permissoes);
    fixture.detectChanges();
    const tela: HTMLElement = fixture.nativeElement;
    const url = '/api/usuarios/permissoes/lote';
    const consulta = http.expectOne({ method: 'GET', url });
    expect(consulta.request.headers.get('Authorization')).toBe('Bearer jwt-teste');
    const dados = {
      usuarios: [
        { id: 4, nome: 'Álvaro', ativo: true, proprioUsuario: false, featureIds: [10, 11, 13] },
        { id: 5, nome: 'Bianca', ativo: true, proprioUsuario: false, featureIds: [10, 13] },
        { id: 1, nome: 'Operador', ativo: true, proprioUsuario: true, featureIds: [10, 11, 13] },
        { id: 6, nome: 'Inativo', ativo: false, proprioUsuario: false, featureIds: [10] },
      ],
      features: [
        { id: 10, nome: 'Usuários', codigo: FEATURES.usuarios, idFeaturePai: null },
        { id: 11, nome: 'Editar', codigo: FEATURES.editarUsuario, idFeaturePai: 10 },
        { id: 12, nome: 'Cadastrar', codigo: FEATURES.cadastrarUsuario, idFeaturePai: 10 },
        { id: 13, nome: 'Permissões', codigo: FEATURES.permissoes, idFeaturePai: 10 },
      ],
      featureIdsGerenciaveis: [10, 11, 12, 13],
    };
    consulta.flush(dados);
    fixture.detectChanges();
    expect(tela.querySelector('.lista')?.textContent).toContain('Operador');
    expect(tela.querySelector('.lista')?.textContent).not.toContain('Inativo');
    const propriaConta = tela.querySelectorAll<HTMLInputElement>('.lista input')[2];
    expect(propriaConta.disabled).toBe(false);
    propriaConta.click();
    fixture.detectChanges();
    expect(propriaConta.checked).toBe(true);
    propriaConta.click();

    const busca = tela.querySelector<HTMLInputElement>('[type="search"]')!;
    busca.value = 'alvaro';
    busca.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(tela.querySelectorAll('.lista input')).toHaveLength(1);
    tela.querySelector<HTMLInputElement>('.lista input')!.click();
    busca.value = '5';
    busca.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    tela.querySelector<HTMLInputElement>('.lista input')!.click();
    fixture.detectChanges();

    const grupo = tela.querySelector<HTMLButtonElement>('.grupo-feature')!;
    grupo.click();
    fixture.detectChanges();
    expect(grupo.getAttribute('aria-expanded')).toBe('false');
    expect(tela.querySelectorAll('.arvore input')).toHaveLength(0);
    const buscarPermissao = tela.querySelectorAll<HTMLInputElement>('[type="search"]')[1];
    buscarPermissao.value = 'editar';
    buscarPermissao.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(grupo.getAttribute('aria-expanded')).toBe('true');
    expect(tela.querySelectorAll('.arvore input')).toHaveLength(1);
    buscarPermissao.value = '';
    buscarPermissao.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(grupo.getAttribute('aria-expanded')).toBe('false');
    grupo.click();
    fixture.detectChanges();

    const filhos = tela.querySelectorAll<HTMLInputElement>('.arvore .modulo input');
    expect(tela.querySelector('.grupo-feature')?.textContent).toContain('Usuários');
    expect(tela.querySelectorAll('.arvore input')).toHaveLength(filhos.length);
    expect(filhos).toHaveLength(3);
    expect(filhos[1].disabled).toBe(false);
    expect(filhos[0].indeterminate).toBe(true);
    expect(filhos[2].checked).toBe(true);
    filhos[0].click();
    filhos[1].click();
    filhos[2].click();
    fixture.detectChanges();

    const dialogo = tela.querySelector('dialog')!;
    dialogo.showModal = () => dialogo.setAttribute('open', '');
    dialogo.close = () => dialogo.removeAttribute('open');
    tela.querySelector<HTMLButtonElement>('.pagina .acoes button')!.click();
    http.expectNone({ method: 'PUT', url });
    expect(dialogo.open).toBe(true);

    const confirmar = dialogo.querySelector<HTMLButtonElement>('.primario')!;
    confirmar.click();
    confirmar.click();
    const lote = http.expectOne({ method: 'PUT', url });
    expect(lote.request.headers.get('Authorization')).toBe('Bearer jwt-teste');
    expect(lote.request.body).toEqual({ usuarioIds: [4, 5], concederIds: [11, 12], revogarIds: [13] });
    lote.flush({ mensagem: 'As permissões foram alteradas simultaneamente.' }, {
      status: 409, statusText: 'Conflict',
    });
    fixture.detectChanges();
    expect(dialogo.querySelector('[role="alert"]')?.textContent).toContain('simultaneamente');
    expect(filhos[0].checked).toBe(true);

    confirmar.click();
    http.expectOne({ method: 'PUT', url }).flush(null, { status: 204, statusText: 'No Content' });
    http.expectOne({ method: 'GET', url }).flush({
      ...dados,
      usuarios: dados.usuarios.map(usuario =>
        usuario.proprioUsuario ? usuario : { ...usuario, featureIds: [10, 11, 12] },
      ),
    });
    fixture.detectChanges();
    expect(dialogo.open).toBe(false);
    const atualizados = tela.querySelectorAll<HTMLInputElement>('.arvore .modulo input');
    expect(atualizados[0].checked).toBe(true);
    expect(atualizados[2].checked).toBe(false);
    expect(tela.querySelector('.aviso')?.textContent).toContain('Permissões atualizadas.');
    http.verify();
  });
});
