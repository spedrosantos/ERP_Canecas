import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { notificacoesInterceptor } from '../../core/interceptors/notificacoes.interceptor';
import { NotificacoesService } from '../../core/services/notificacoes.service';
import { Financeiro } from './financeiro';
import { Baixa, TipoTitulo, Titulo } from './financeiro.service';

const base = '/api/financeiro';
const pagar: Titulo = {
  id: 1, tipo: 'PAGAR', compraId: 7, vendaId: null, fornecedorId: 9, nomeFornecedor: 'Fornecedor',
  clienteId: null, nomeCliente: null, valor: 100, valorBaixado: 0, saldo: 100,
  situacao: 'PENDENTE', vencimento: '2026-12-02', usuarioId: 1,
  criadoEm: '2026-11-25T15:00:00Z', canceladoEm: null,
};
const baixa: Baixa = {
  id: 5, tituloId: 1, valor: 40, formaPagamento: 'PIX', usuarioId: 1, observacao: null,
  criadoEm: '2026-11-25T16:00:00Z', estornadoEm: null, usuarioEstornoId: null, motivoEstorno: null,
};
const consolidado = { totalTitulos: 2, valorTotal: 100, valorBaixado: 45, saldo: 55, saldoVencido: 10 };
const quitados = { totalTitulos: 1, valorTotal: 30, valorBaixado: 30, saldo: 0, saldoVencido: 0 };

function montar(tipo: TipoTitulo) {
  TestBed.configureTestingModule({
    imports: [Financeiro],
    providers: [
      provideHttpClient(withInterceptors([notificacoesInterceptor])), provideHttpClientTesting(),
      { provide: ActivatedRoute, useValue: { snapshot: { data: { tipo } } } },
    ],
  });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(Financeiro);
  const tela: HTMLElement = fixture.nativeElement;

  function responderLista(titulo: Titulo, situacao: string | null = null, codigo?: string) {
    const chave = tipo === 'PAGAR' ? 'fornecedorId' : 'clienteId';
    const consulta = http.expectOne(r => r.url === base + '/titulos');
    expect(consulta.request.params.get('tipo')).toBe(tipo);
    expect(consulta.request.params.get('situacao')).toBe(situacao);
    if (codigo) expect(consulta.request.params.get(chave)).toBe(codigo);
    consulta.flush({ total: 1, pagina: 1, tamanhoPagina: 20, itens: [titulo] });
    const principal = http.expectOne(r => r.url === base + '/indicadores' && r.params.get('situacao') === situacao);
    if (codigo) expect(principal.request.params.get(chave)).toBe(codigo);
    const agrupar = (valor: typeof consolidado) => ({ contasAPagar: valor, contasAReceber: valor });
    principal.flush(agrupar(consolidado));
    if (!situacao) {
      http.expectOne(r => r.url === base + '/indicadores' && r.params.get('situacao') === 'QUITADO')
        .flush(agrupar(quitados));
    } else {
      http.expectNone(r => r.url === base + '/indicadores' && r.params.get('situacao') === 'QUITADO');
    }
    fixture.detectChanges();
  }

  function preencher(seletor: string, valor: string, evento = 'input') {
    const campo = tela.querySelector<HTMLInputElement>(seletor)!;
    campo.value = valor;
    campo.dispatchEvent(new Event(evento));
    fixture.detectChanges();
  }

  return { http, tela, fixture, responderLista, preencher };
}

describe('Financeiro', () => {
  it('filtra títulos e indicadores pelo mesmo cliente e pela mesma situação', async () => {
    const { tela, http, fixture, responderLista, preencher } = montar('RECEBER');
    const receber: Titulo = {
      ...pagar, tipo: 'RECEBER', compraId: null, vendaId: 6, clienteId: 12,
      nomeCliente: 'Cliente de teste', fornecedorId: null, nomeFornecedor: null, situacao: 'VENCIDO',
    };
    responderLista(receber);
    await fixture.whenStable();
    expect(tela.querySelector('h1')?.textContent).toBe('Contas a Receber');
    expect(tela.querySelector('tbody')?.textContent).toContain('Venda #6');
    expect(tela.querySelector('.situacao')?.classList.contains('negativa')).toBe(true);
    expect(tela.querySelector('.indicadores')?.textContent).toContain('30,00');
    preencher('[name="codigo"]', '12');
    preencher('[name="situacao"]', 'VENCIDO', 'change');
    tela.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();
    responderLista(receber, 'VENCIDO', '12');
    expect(tela.querySelector('.indicadores')?.textContent).toContain('0,00');
    http.verify();
  });

  it('registra uma baixa parcial, mostra conflito de saldo e estorna com justificativa', async () => {
    const { tela, http, fixture, responderLista, preencher } = montar('PAGAR');
    responderLista(pagar);
    tela.querySelector<HTMLButtonElement>('button[title="Ver título"]')!.click();
    http.expectOne(base + '/titulos/1').flush({ titulo: pagar, baixas: [] });
    fixture.detectChanges();

    const dialogo = tela.querySelector('dialog')!;
    dialogo.showModal = () => dialogo.setAttribute('open', '');
    dialogo.close = () => dialogo.removeAttribute('open');
    tela.querySelector<HTMLButtonElement>('.detalhe .cabecalho .primario')!.click();
    fixture.detectChanges();
    await fixture.whenStable();
    preencher('dialog input', '4000');
    preencher('dialog select', 'PIX', 'change');
    const enviar = () => dialogo.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    enviar();
    const rejeitada = http.expectOne({ method: 'POST', url: base + '/titulos/1/baixas' });
    expect(rejeitada.request.body).toEqual({ valor: 40, formaPagamento: 'PIX', observacao: null });
    rejeitada.flush({ mensagem: 'O valor da baixa excede o saldo do título.' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(dialogo.textContent).toContain('excede o saldo');
    enviar();
    http.expectOne({ method: 'POST', url: base + '/titulos/1/baixas' }).flush(baixa);
    const parcial = { ...pagar, saldo: 60, valorBaixado: 40, situacao: 'PARCIAL' as const };
    http.expectOne(base + '/titulos/1').flush({ titulo: parcial, baixas: [baixa] });
    responderLista(parcial);
    expect(TestBed.inject(NotificacoesService).itens().some(x => x.mensagem === 'Pagamento registrado com sucesso.')).toBe(true);
    expect(tela.querySelector('.resumo .situacao')?.classList.contains('parcial')).toBe(true);

    tela.querySelector<HTMLButtonElement>('[title="Estornar baixa"]')!.click();
    fixture.detectChanges();
    await fixture.whenStable();
    enviar();
    http.expectNone(base + '/titulos/1/baixas/5/estornar');
    preencher('dialog textarea', ' Lançamento duplicado ');
    enviar();
    const reversao = http.expectOne({ method: 'POST', url: base + '/titulos/1/baixas/5/estornar' });
    expect(reversao.request.body).toEqual({ motivo: 'Lançamento duplicado' });
    const estornada = { ...baixa, estornadoEm: baixa.criadoEm, usuarioEstornoId: 1, motivoEstorno: 'Lançamento duplicado' };
    reversao.flush(estornada);
    http.expectOne(base + '/titulos/1').flush({ titulo: pagar, baixas: [estornada] });
    responderLista(pagar);
    expect(tela.querySelector('tbody')?.textContent).toContain('Estornado');
    expect(tela.querySelector('[title="Estornar baixa"]')).toBeNull();
    http.verify();
  });
});
