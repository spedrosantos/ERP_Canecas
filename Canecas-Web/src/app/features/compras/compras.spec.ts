import { vi } from 'vitest';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Compras } from './compras';
import { PedidoDetalhe } from './compras.service';

const api = '/api/compras/pedidos';
const detalhe: PedidoDetalhe = {
  pedido: {
    id: 12, fornecedorId: 5, usuarioId: 3, situacao: 'PENDENTE', valorTotal: 7.4,
    observacao: null, criadoEm: '2026-11-25T14:30:00Z', atualizadoEm: null, recebidoEm: null
  },
  itens: [{ id: 1, produtoId: 8, quantidade: 2, custoUnitario: 3.7 }]
};
const pagina = { pagina: 1, tamanhoPagina: 20, itens: [detalhe.pedido], total: 1 };

describe('Compras', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Compras],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
  });

  it('sugere cadastros e atualiza o resumo ao adicionar, editar e remover um item', async () => {
    const fixture = TestBed.createComponent(Compras);
    const http = TestBed.inject(HttpTestingController);
    const tela: HTMLElement = fixture.nativeElement;
    http.expectOne(`${api}?pagina=1&tamanhoPagina=20`).flush(pagina);
    fixture.detectChanges();
    tela.querySelector<HTMLButtonElement>('.cabecalho button')!.click();
    fixture.detectChanges();

    const fornecedor = tela.querySelector<HTMLInputElement>('#fornecedores-busca')!;
    fornecedor.dispatchEvent(new Event('focus'));
    await vi.waitFor(() => {
      const req = http.expectOne(r => r.url === `${api}/fornecedores` && r.params.get('busca') === '');
      req.flush({ itens: [{ id: 5, nome: 'Fornecedor A' }] });
    });
    fixture.detectChanges();
    fornecedor.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    fornecedor.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    fixture.detectChanges();
    expect(fornecedor.value).toBe('Fornecedor A');

    const produto = tela.querySelector<HTMLInputElement>('#produtos-busca')!;
    produto.value = 'Caneca';
    produto.dispatchEvent(new Event('input'));
    await vi.waitFor(() => {
      const req = http.expectOne(r => r.url === `${api}/produtos` && r.params.get('busca') === 'Caneca');
      req.flush({ itens: [{ id: 8, nome: 'Caneca' }] });
    });
    fixture.detectChanges();
    tela.querySelector<HTMLElement>('#produtos-opcao-0')!.click();
    fixture.detectChanges();
    tela.querySelector<HTMLButtonElement>('.adicionar')!.click();
    fixture.detectChanges();
    expect(tela.querySelector('.nome-produto')?.textContent).toContain('Caneca');

    await fixture.whenStable();
    const quantidade = tela.querySelector<HTMLInputElement>('[aria-label="Quantidade de Caneca"]')!;
    const custo = tela.querySelector<HTMLInputElement>('[aria-label="Custo unitário de Caneca"]')!;
    quantidade.value = '2.5';
    quantidade.dispatchEvent(new Event('input'));
    custo.value = '3.125';
    custo.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(tela.querySelector('td.subtotal')?.textContent).toContain('7,81');
    expect(tela.querySelector('.fechamento-pedido')?.textContent).toContain('2,5');
    expect(tela.querySelector('.total-pedido')?.textContent).toContain('7,81');

    tela.querySelector<HTMLButtonElement>('.remover')!.click();
    fixture.detectChanges();
    expect(tela.querySelector('.nome-produto')).toBeNull();
    expect(tela.querySelector('.total-pedido')?.textContent).toContain('0,00');
    http.verify();
  });

  it('confirma o recebimento integral e retira ações de um pedido recebido', () => {
    const fixture = TestBed.createComponent(Compras);
    const http = TestBed.inject(HttpTestingController);
    const tela: HTMLElement = fixture.nativeElement;
    const clicar = (texto: string) => {
      const botao = Array.from(tela.querySelectorAll('button')).find(item => item.textContent.trim() === texto);
      expect(botao).toBeDefined();
      botao!.click();
      fixture.detectChanges();
    };

    http.expectOne(`${api}?pagina=1&tamanhoPagina=20`).flush(pagina);
    fixture.detectChanges();
    clicar('Abrir');
    http.expectOne(`${api}/12`).flush(detalhe);
    fixture.detectChanges();

    clicar('Receber pedido');
    http.expectNone(`${api}/12/receber`);
    clicar('Confirmar recebimento');
    const recebimento = http.expectOne({ method: 'POST', url: `${api}/12/receber` });
    expect(recebimento.request.body).toBeNull();
    recebimento.flush({
      ...detalhe,
      pedido: { ...detalhe.pedido, situacao: 'RECEBIDO', recebidoEm: '2026-11-25T15:00:00Z' }
    });
    http.expectOne(`${api}?pagina=1&tamanhoPagina=20`).flush(pagina);
    fixture.detectChanges();

    expect(tela.textContent).toContain('Pedido recebido e estoque atualizado.');
    expect(tela.textContent).not.toContain('Editar');
    expect(tela.textContent).not.toContain('Cancelar pedido');
    expect(tela.textContent).not.toContain('Receber pedido');
    http.verify();
  });
});
