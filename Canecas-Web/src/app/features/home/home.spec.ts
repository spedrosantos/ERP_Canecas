import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { FEATURES, FeatureService } from '../../core/services/feature.service';
import { Home } from './home';

const urlVendas = '/api/vendas/pedidos/dashboard';
const urlCompras = '/api/compras/pedidos/dashboard';
const urlFinanceiro = '/api/financeiro/indicadores';
const urlEstoque = '/api/estoque/saldos';

const vendas = {
  faturamento: 120, vendasConfirmadas: 2, lucroBruto: 92, margemBruta: 76.67,
  evolucao: [{ mes: '2024-03-01', valor: 120 }],
  produtosMaisVendidos: [{ produtoId: 1, nomeProduto: 'Caneca', quantidade: 2 }],
  ultimasVendas: [{ id: 4, clienteId: 3, nomeCliente: 'Cliente de teste', confirmadoEm: '2024-03-01T03:00:00Z', valor: 70 }],
};

function montar(...codigos: string[]) {
  const permissoes = signal(new Set(codigos));
  TestBed.configureTestingModule({
    imports: [Home],
    providers: [
      provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
      { provide: AuthService, useValue: { usuario: () => ({ nome: 'Ana' }) } },
      { provide: FeatureService, useValue: { prontas: signal(true), permiteRota: (codigo: string) => permissoes().has(codigo) } },
    ],
  });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(Home);
  fixture.detectChanges();
  const tela: HTMLElement = fixture.nativeElement;
  const requisicao = (url: string) => http.expectOne(request => request.url === url);
  return { fixture, tela, http, permissoes, requisicao };
}

describe('Dashboard', () => {
  it('consulta só os módulos autorizados e oculta dados quando a permissão é removida', () => {
    const { fixture, tela, http, permissoes, requisicao } = montar(FEATURES.vendas);
    const consulta = requisicao(urlVendas);
    expect(consulta.request.params.get('dataInicio')).toMatch(/^\d{4}-\d{2}-01$/);
    http.expectNone(request => [urlCompras, urlFinanceiro, urlEstoque].includes(request.url));
    consulta.flush(vendas);
    fixture.detectChanges();
    expect(tela.textContent).toContain('120,00');
    expect(tela.textContent).toContain('76,67%');
    expect(tela.textContent).toContain('Caneca');
    expect(tela.textContent).toContain('Cliente de teste');
    expect(tela.querySelector('.legenda')?.textContent).not.toContain('Compras');

    permissoes.set(new Set());
    fixture.detectChanges();
    expect(tela.textContent).not.toContain('Caneca');
    expect(tela.textContent).toContain('Seu perfil ainda não tem acesso');
    http.verify();
  });

  it('aplica o período aos indicadores, mantém a posição de estoque e trata falhas independentes', async () => {
    const { fixture, tela, http, requisicao } = montar(FEATURES.vendas, FEATURES.compras, FEATURES.financeiro, FEATURES.estoque);
    requisicao(urlVendas).flush(vendas);
    requisicao(urlCompras).flush({ totalCompras: 0, evolucao: [] });
    const saldo = { totalTitulos: 2, valorTotal: 100, valorBaixado: 60, saldo: 40, saldoVencido: 10 };
    requisicao(urlFinanceiro).flush({ contasAPagar: saldo, contasAReceber: saldo });
    const estoque = requisicao(urlEstoque);
    expect(estoque.request.params.get('abaixoDoMinimo')).toBe('true');
    expect(estoque.request.params.get('tamanhoPagina')).toBe('5');
    estoque.flush({ total: 0, pagina: 1, tamanhoPagina: 5, itens: [] });
    fixture.detectChanges();
    await fixture.whenStable();

    for (const [nome, valor] of [['dataInicio', '2024-03-01'], ['dataFim', '2024-03-31']]) {
      const campo = tela.querySelector<HTMLInputElement>('input[name="' + nome + '"]')!;
      campo.value = valor;
      campo.dispatchEvent(new Event('input'));
    }
    tela.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();

    const novaVenda = requisicao(urlVendas);
    expect(novaVenda.request.params.get('dataInicio')).toBe('2024-03-01');
    novaVenda.flush({ ...vendas, faturamento: 0, vendasConfirmadas: 0, lucroBruto: 0, margemBruta: null,
      produtosMaisVendidos: [], ultimasVendas: [], evolucao: [] });
    requisicao(urlCompras).flush({}, { status: 503, statusText: 'Indisponível' });
    const financeiro = requisicao(urlFinanceiro);
    expect(financeiro.request.params.get('vencimentoFim')).toBe('2024-03-31');
    expect(financeiro.request.params.has('tipo')).toBe(false);
    financeiro.flush({ contasAPagar: saldo, contasAReceber: saldo });
    http.expectNone(request => request.url === urlEstoque);

    fixture.detectChanges();
    expect(tela.textContent).toContain('Não foi possível carregar as compras.');
    expect(tela.textContent).toContain('Nenhuma venda confirmada no período.');
    expect(tela.textContent).toContain('Nenhum produto abaixo do mínimo.');
    expect(tela.textContent).toContain('40,00');
    expect(tela.querySelector('.legenda')?.textContent).toContain('Vendas');
    http.verify();
  });
});
