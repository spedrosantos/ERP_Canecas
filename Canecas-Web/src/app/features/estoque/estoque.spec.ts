import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Estoque } from './estoque';
import { SaldoEstoque } from './estoque.model';

const saldo: SaldoEstoque = {
  produtoId: 1,
  nomeProduto: 'Caneca',
  unidadeMedida: 'UN',
  produtoAtivo: true,
  quantidade: 2,
  estoqueMinimo: 1,
  custoMedio: 10,
  atualizadoEm: null,
};

describe('Estoque', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Estoque],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('mantém as colunas alinhadas e oferece somente uma saída para produto inativo', () => {
    const fixture = TestBed.createComponent(Estoque);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    http.expectOne('/api/estoque/saldos?pagina=1&tamanhoPagina=20').flush({
      itens: [saldo, { ...saldo, produtoId: 2, produtoAtivo: false }],
      total: 2, pagina: 1, tamanhoPagina: 20,
    });
    fixture.detectChanges();

    const tela: HTMLElement = fixture.nativeElement;
    const linhas = tela.querySelectorAll('tbody tr');
    for (const linha of linhas) {
      expect(linha.children).toHaveLength(tela.querySelectorAll('thead th').length);
    }

    expect(linhas[0].querySelectorAll('button svg')).toHaveLength(2);
    expect(linhas[1].querySelectorAll('button svg')).toHaveLength(1);
    const dialogo = tela.querySelector<HTMLDialogElement>('#titulo-ajuste')!.closest('dialog')!;
    dialogo.showModal = () => dialogo.setAttribute('open', '');
    linhas[1].querySelector('button')!.click();
    fixture.detectChanges();

    const tipo = dialogo.querySelector<HTMLSelectElement>('select')!;
    expect(tipo.value).toBe('SAIDA');
    expect(Array.from(tipo.options).map(opcao => opcao.value)).toEqual(['SAIDA']);
    http.verify();
  });

  it('preenche os centavos da direita para a esquerda e envia um número', () => {
    const fixture = TestBed.createComponent(Estoque);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/estoque/saldos?pagina=1&tamanhoPagina=20').flush({
      itens: [saldo], total: 1, pagina: 1, tamanhoPagina: 20,
    });
    fixture.detectChanges();

    const tela: HTMLElement = fixture.nativeElement;
    const dialogo = tela.querySelector<HTMLDialogElement>('dialog')!;
    dialogo.showModal = () => dialogo.setAttribute('open', '');
    tela.querySelector<HTMLButtonElement>('tbody button')!.click();
    fixture.detectChanges();

    const custo = dialogo.querySelector<HTMLInputElement>('[formControlName="custoUnitario"]')!;
    expect(custo.value).toMatch(/^R\$\s10,00$/);
    const controle = fixture.componentInstance.formularioAjuste.controls.custoUnitario;
    controle.setValue(0);
    custo.dispatchEvent(new Event('focus'));
    expect(custo.selectionStart).toBe(0);
    expect(custo.selectionEnd).toBe(custo.value.length);

    custo.value = '1';
    custo.dispatchEvent(new Event('input'));
    expect(custo.value).toMatch(/^R\$\s0,01$/);
    custo.value += '2';
    custo.dispatchEvent(new Event('input'));
    expect(custo.value).toMatch(/^R\$\s0,12$/);
    custo.value += '3';
    custo.dispatchEvent(new Event('input'));
    custo.dispatchEvent(new Event('blur'));
    expect(custo.value).toMatch(/^R\$\s1,23$/);
    const quantidade = dialogo.querySelector<HTMLInputElement>('[formControlName="quantidade"]')!;
    quantidade.value = '1234,567';
    quantidade.dispatchEvent(new Event('input'));
    quantidade.dispatchEvent(new Event('blur'));
    expect(quantidade.value).toBe('1.234,567');
    fixture.componentInstance.formularioAjuste.controls.justificativa.setValue('Entrada inicial');
    fixture.componentInstance.salvarAjuste();

    const ajuste = http.expectOne({ method: 'POST', url: '/api/estoque/movimentacoes/ajuste' });
    expect(ajuste.request.body.custoUnitario).toBe(1.23);
    expect(ajuste.request.body.quantidade).toBe(1234.567);
    ajuste.flush({ mensagem: 'Falha de teste' }, { status: 400, statusText: 'Bad Request' });
    http.verify();
  });

  it('cancela a consulta anterior e exibe a referência de um ajuste já estornado', () => {
    const fixture = TestBed.createComponent(Estoque);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    const consultaInicial = http.expectOne('/api/estoque/saldos?pagina=1&tamanhoPagina=20');
    fixture.componentInstance.trocarAba('movimentacoes');
    expect(consultaInicial.cancelled).toBe(true);

    http.expectOne('/api/estoque/movimentacoes?pagina=1&tamanhoPagina=20').flush({
      itens: [{
        id: 1, produtoId: 1, nomeProduto: 'Caneca', nomeUsuario: 'Operador',
        origem: 'AJUSTE', tipo: 'ENTRADA', quantidade: 2, custoUnitario: 10,
        estornoId: 2, criadoEm: '2026-11-25T15:00:00Z',
      }],
      total: 1, pagina: 1, tamanhoPagina: 20,
    });
    fixture.detectChanges();

    const tela: HTMLElement = fixture.nativeElement;
    const linha = tela.querySelector('tbody tr')!;
    expect(linha.textContent).toContain('Operador');
    expect(linha.textContent).toContain('Estornado · 2');
    expect(linha.querySelector('button')).toBeNull();
    http.verify();
  });
});
