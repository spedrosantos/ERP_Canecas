import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { FEATURES, FeatureService } from '../../core/services/feature.service';
import { Clientes } from './clientes';
import { documentoValido, formatarDocumento } from '../../shared/validations/documentos';

describe('Clientes', () => {
  afterEach(() => vi.useRealTimers());

  it('formata e valida CPF e CNPJ alfanumérico', () => {
    expect(formatarDocumento('52998224725', 'F')).toBe('529.982.247-25');
    expect(documentoValido('529.982.247-25', 'F')).toBe(true);
    expect(documentoValido('111.111.111-11', 'F')).toBe(false);
    expect(formatarDocumento('12abc34501de35', 'J')).toBe('12.ABC.345/01DE-35');
    expect(documentoValido('12.abc.345/01de-35', 'J')).toBe(true);
    expect(documentoValido('12.ABC.345/01DE-36', 'J')).toBe(false);
  });

  it('preserva dados manuais sem confirmação e permite cadastrar após falha da consulta', () => {
    vi.useFakeTimers({ toFake: ['Date', 'setInterval', 'clearInterval'] });
    TestBed.configureTestingModule({
      imports: [Clientes],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: FeatureService,
          useValue: { tem: (codigo: string) => codigo === FEATURES.clientes },
        },
      ],
    });
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(Clientes);
    fixture.detectChanges();
    const tela: HTMLElement = fixture.nativeElement;
    const listar = () => http.expectOne('/api/clientes?pagina=1&tamanhoPagina=20&ativo=true');
    listar().flush({ itens: [], total: 0, pagina: 1, tamanhoPagina: 20 });
    fixture.detectChanges();

    for (const dialogo of tela.querySelectorAll('dialog')) {
      dialogo.showModal = () => dialogo.setAttribute('open', '');
      dialogo.close = () => dialogo.removeAttribute('open');
    }

    const preencher = (seletor: string, valor: string, colado = false) => {
      const campo = tela.querySelector<HTMLInputElement>(seletor)!;
      campo.value = valor;
      campo.dispatchEvent(new InputEvent('input', { inputType: colado ? 'insertFromPaste' : 'insertText' }));
      fixture.detectChanges();
      return campo;
    };
    tela.querySelector<HTMLButtonElement>('.cabecalho button')!.click();
    const tipo = tela.querySelector<HTMLSelectElement>('dialog select')!;
    tipo.value = 'J';
    tipo.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    const nome = preencher('[formControlName="nome"]', 'Nome manual');
    expect(preencher('#cliente-documento', '12abc34501de35', true).value).toBe('12.ABC.345/01DE-35');
    vi.advanceTimersByTime(349);
    http.expectNone('/api/consultas/cnpj?cnpj=12ABC34501DE35');
    vi.advanceTimersByTime(1);
    const desatualizada = http.expectOne('/api/consultas/cnpj?cnpj=12ABC34501DE35');
    preencher('#cliente-documento', '12abc34501de3');
    expect(desatualizada.cancelled).toBe(true);
    vi.advanceTimersByTime(350);
    http.expectNone(requisicao => requisicao.url === '/api/consultas/cnpj');
    preencher('#cliente-documento', '12abc34501de35');
    vi.advanceTimersByTime(350);
    http.expectOne('/api/consultas/cnpj?cnpj=12ABC34501DE35').flush({
      cnpj: '12ABC34501DE35', razaoSocial: 'Nome consultado', nomeFantasia: 'Fantasia consultada',
      endereco: {
        cep: null, logradouro: null, numero: null, complemento: null, bairro: null, cidade: null, uf: null,
      },
    });
    fixture.detectChanges();
    const confirmacao = tela.querySelector<HTMLDialogElement>('dialog:not(.editor)')!;
    expect(confirmacao.open).toBe(true);
    expect(nome.value).toBe('Nome manual');
    expect(tela.querySelector<HTMLInputElement>('[formControlName="nomeFantasia"]')?.value)
      .toBe('Fantasia consultada');
    confirmacao.querySelector<HTMLButtonElement>('button')!.click();

    preencher('#cliente-cep', '01001-000');
    tela.querySelector<HTMLButtonElement>('.com-consulta button')!.click();
    http.expectOne('/api/consultas/cep?cep=01001000').flush(null, {
      status: 503, statusText: 'Service Unavailable',
    });
    fixture.detectChanges();
    tela.querySelector('form[novalidate]')!.dispatchEvent(new Event('submit', { cancelable: true }));
    const cadastro = http.expectOne({ method: 'POST', url: '/api/clientes' });
    expect(cadastro.request.body).toMatchObject({
      tipoPessoa: 'J', nome: 'Nome manual', nomeFantasia: 'Fantasia consultada',
      cpfCnpj: '12ABC34501DE35', cep: '01001000',
    });
    cadastro.flush({ id: 1, ...cadastro.request.body });
    listar().flush({ itens: [], total: 0, pagina: 1, tamanhoPagina: 20 });
    http.verify();
  });
});
