import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Vendas } from './vendas';
import { PedidoDetalhe, URL_VENDAS } from './vendas.service';

const detalhe: PedidoDetalhe = {
  pedido: {
    id: 1, clienteId: 4, usuarioId: 1, situacao: 'PENDENTE', valorTotal: 12.5,
    observacao: 'Retirada', criadoEm: '2026-11-25T15:00:00Z', atualizadoEm: null,
    confirmadoEm: null, canceladoEm: null,
  },
  nomeCliente: 'Cliente cadastrado',
  itens: [{
    id: 1, produtoId: 8, quantidade: 2.5, precoUnitario: 5, custoUnitario: null,
    nomeProduto: 'Caneca', unidadeMedida: 'UN',
  }],
};
const pagina = { pagina: 1, tamanhoPagina: 20, total: 1, itens: [detalhe.pedido] };

describe('Vendas', () => {
  let fixture: ComponentFixture<Vendas>;
  let http: HttpTestingController;
  let tela: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Vendas],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Vendas);
    tela = fixture.nativeElement;
    listar().flush(pagina);
    fixture.detectChanges();
  });

  function listar() {
    return http.expectOne(request => request.url === URL_VENDAS && request.method === 'GET');
  }

  function clicar(texto: string, escopo: ParentNode = tela): void {
    const botao = Array.from(escopo.querySelectorAll('button')).find(item => item.textContent.trim() === texto);
    expect(botao).toBeDefined();
    botao!.click();
    fixture.detectChanges();
  }

  function preencher(seletor: string, texto: string): void {
    const campo = tela.querySelector<HTMLInputElement | HTMLTextAreaElement>(seletor)!;
    campo.value = texto;
    campo.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  async function selecionar(tipo: 'clientes' | 'produtos', opcao: object): Promise<void> {
    tela.querySelector<HTMLInputElement>('#' + tipo + '-busca')!.dispatchEvent(new Event('focus'));
    await new Promise(resolve => setTimeout(resolve, 0));
    http.expectOne(request => request.url === URL_VENDAS + '/' + tipo).flush({ itens: [opcao] });
    fixture.detectChanges();
    tela.querySelector<HTMLElement>('#' + tipo + '-opcao-0')!.click();
    fixture.detectChanges();
  }

  function saldo(produtoId = 8) {
    http.expectOne(URL_VENDAS + '/produtos/' + produtoId + '/disponibilidade').flush({
      produtoId, quantidadeDisponivel: 9, nomeProduto: 'Caneca', unidadeMedida: 'UN',
    });
    fixture.detectChanges();
  }

  function abrirVenda(): void {
    tela.querySelector<HTMLButtonElement>('[aria-label="Ver venda #1"]')!.click();
    http.expectOne(URL_VENDAS + '/1').flush(detalhe);
    fixture.detectChanges();
  }

  it('seleciona cadastros, exibe o saldo, calcula o total e envia os itens sem total no DTO', async () => {
    clicar('Nova venda');
    await selecionar('clientes', { id: 4, nome: 'Cliente cadastrado' });
    await selecionar('produtos', { id: 8, nome: 'Caneca', unidadeMedida: 'UN' });
    clicar('Adicionar');
    saldo();
    expect(tela.querySelector('.nome-produto')?.textContent).toContain('Disponível: 9 UN');

    await fixture.whenStable();
    preencher('[aria-label="Quantidade de Caneca"]', '2,5');
    preencher('[aria-label="Preço unitário de Caneca"]', '500');
    preencher('[name="observacao"]', 'Retirada');
    expect(tela.querySelector('.total-pedido')?.textContent).toContain('12,50');

    tela.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    const salvar = http.expectOne({ method: 'POST', url: URL_VENDAS });
    expect(salvar.request.body).toEqual({
      clienteId: 4, observacao: 'Retirada',
      itens: [{ produtoId: 8, quantidade: 2.5, precoUnitario: 5 }],
    });
    salvar.flush(detalhe);
    listar().flush(pagina);
    fixture.detectChanges();
    expect(tela.textContent).toContain('Venda salva.');
  });

  it('edita uma venda pendente e consulta o saldo dos itens carregados', async () => {
    abrirVenda();
    clicar('Editar');
    saldo();
    await fixture.whenStable();
    expect(tela.querySelector<HTMLInputElement>('#clientes-busca')?.value).toBe('Cliente cadastrado');
    preencher('[aria-label="Quantidade de Caneca"]', '3');
    tela.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    const editar = http.expectOne({ method: 'PUT', url: URL_VENDAS + '/1' });
    expect(editar.request.body.itens).toEqual([{ produtoId: 8, quantidade: 3, precoUnitario: 5 }]);
    editar.flush(detalhe);
    listar().flush(pagina);
  });

  it('pede confirmação das ações e mostra erros de estoque no modal', () => {
    abrirVenda();
    const dialogo = tela.querySelector('dialog')!;
    dialogo.showModal = () => dialogo.setAttribute('open', '');
    dialogo.close = () => dialogo.removeAttribute('open');
    const acoes = tela.querySelector('.conteudo > .acoes')!;

    clicar('Confirmar venda', acoes);
    expect(dialogo.open).toBe(true);
    http.expectNone(URL_VENDAS + '/1/confirmar');
    clicar('Confirmar venda', dialogo);
    http.expectOne({ method: 'POST', url: URL_VENDAS + '/1/confirmar' }).flush(
      { mensagem: 'Saldo insuficiente para o produto 8.' }, { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();
    expect(dialogo.querySelector('[role="alert"]')?.textContent).toContain('Saldo insuficiente');
    expect(dialogo.open).toBe(true);

    clicar('Confirmar venda', dialogo);
    const confirmada: PedidoDetalhe = {
      ...detalhe, pedido: { ...detalhe.pedido, situacao: 'CONFIRMADO', confirmadoEm: '2026-11-25T16:00:00Z' },
    };
    http.expectOne({ method: 'POST', url: URL_VENDAS + '/1/confirmar' }).flush(confirmada);
    listar().flush(pagina);
    fixture.detectChanges();
    expect(dialogo.open).toBe(false);
    expect(tela.textContent).toContain('Venda confirmada e estoque atualizado.');

    clicar('Cancelar venda', acoes);
    expect(dialogo.textContent).toContain('devolvidos ao estoque');
    http.expectNone(URL_VENDAS + '/1/cancelar');
    clicar('Confirmar cancelamento', dialogo);
    http.expectOne({ method: 'POST', url: URL_VENDAS + '/1/cancelar' }).flush({
      ...confirmada, pedido: { ...confirmada.pedido, situacao: 'CANCELADO', canceladoEm: '2026-11-25T17:00:00Z' },
    });
    listar().flush(pagina);
    fixture.detectChanges();
    expect(tela.querySelector('dialog')).toBeNull();
    expect(tela.textContent).toContain('Venda cancelada.');
  });

  afterEach(() => http.verify());
});
