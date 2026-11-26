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
