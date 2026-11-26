import { CurrencyPipe, DecimalPipe, registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { Component, LOCALE_ID, WritableSignal, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable, Subscription, catchError, forkJoin, of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { FEATURES, FeatureService } from '../../core/services/feature.service';
import { TemaService } from '../../core/services/tema.service';
import { AtalhoPeriodo, granularidadeDiaria, periodoAnterior, periodoPredefinido } from './dashboard-periodo';
import { graficoComprasVendas, graficoFaturamento, graficoFinanceiro, graficoRanking } from './dashboard-graficos';
import { GraficoDashboard } from './grafico-dashboard';
import { ResultadoPaginado, SaldoEstoque } from '../estoque/estoque.model';
import { IndicadorFinanceiro } from '../financeiro/financeiro.service';
import { DashboardService, PeriodoDashboard, ResumoCompras, ResumoVendas } from './dashboard.service';

registerLocaleData(localePt, 'pt-BR');

const dataConfirmacao = new Intl.DateTimeFormat('pt-BR', {
  timeZone: 'America/Sao_Paulo', day: '2-digit', month: '2-digit', year: 'numeric',
});

type Consulta<T> = { estado: 'carregando' | 'erro' } | { estado: 'pronto'; dados: T };
type ComComparacao<T> = { dados: T; anterior: T | null };
type Indicador = { titulo: string; icone: string; valor: number | null; anterior?: number | null; margem?: boolean; despesa?: boolean };
type ConsolidadoFinanceiro = { contasAPagar: IndicadorFinanceiro; contasAReceber: IndicadorFinanceiro };

@Component({
  selector: 'app-home',
  imports: [CurrencyPipe, DecimalPipe, FormsModule, RouterLink, GraficoDashboard],
  providers: [{ provide: LOCALE_ID, useValue: 'pt-BR' }],
  templateUrl: './home.html',
  styleUrls: ['./home.scss', './home-grafico.scss'],
})
export class Home {
  protected readonly auth = inject(AuthService);
  private readonly features = inject(FeatureService);
  private readonly dashboard = inject(DashboardService);
  private readonly tema = inject(TemaService);

  protected readonly permissoesProntas = this.features.prontas;
  protected readonly podeVerVendas = computed(() => this.features.permiteRota(FEATURES.vendas));
  protected readonly podeVerCompras = computed(() => this.features.permiteRota(FEATURES.compras));
  protected readonly podeVerFinanceiro = computed(() => this.features.permiteRota(FEATURES.financeiro));
  protected readonly podeVerEstoque = computed(() => this.features.permiteRota(FEATURES.estoque));
  protected readonly temIndicadores = computed(() =>
    this.podeVerVendas() || this.podeVerCompras() || this.podeVerFinanceiro() || this.podeVerEstoque(),
  );

  protected readonly atalhos: { id: AtalhoPeriodo; texto: string }[] = [
    { id: 'hoje', texto: 'Hoje' }, { id: 'semana', texto: 'Últimos 7 dias' }, { id: 'mes', texto: 'Este mês' },
    { id: 'mesAnterior', texto: 'Mês anterior' }, { id: 'personalizado', texto: 'Personalizado' },
  ];
  protected readonly atalho = signal<AtalhoPeriodo>('mes');
  protected filtro = periodoPredefinido('mes');
  protected readonly periodo = signal(this.filtro);
  private readonly comparacao = computed(() => periodoAnterior(this.periodo(), this.atalhoAplicado()));
  protected readonly periodoComparacao = this.comparacao;
  private readonly atalhoAplicado = signal<AtalhoPeriodo>('mes');
  protected readonly diaria = computed(() => granularidadeDiaria(this.periodo()));
  protected readonly erroPeriodo = signal('');
  private readonly atualizacao = signal(0);
  private readonly atualizacaoEstoque = signal(0);
  protected readonly paginaEstoque = signal(1);
  protected readonly vendas = signal<Consulta<ComComparacao<ResumoVendas>> | null>(null);
  protected readonly compras = signal<Consulta<ComComparacao<ResumoCompras>> | null>(null);
  protected readonly financeiro = signal<Consulta<ComComparacao<ConsolidadoFinanceiro>> | null>(null);
  protected readonly estoque = signal<Consulta<ResultadoPaginado<SaldoEstoque>> | null>(null);

  private readonly cores = computed(() => {
    const tema = this.tema.temaAtual();
    return [this.misturar(tema.corPrimaria, '#173b70', .48), '#138173',
      this.misturar(tema.corSecundaria, '#c07b32', .66)];
  });

  protected readonly vendasAtuais = computed(() => this.pronto(this.vendas(), this.podeVerVendas())?.dados);
  protected readonly comprasAtuais = computed(() => this.pronto(this.compras(), this.podeVerCompras())?.dados);
  protected readonly financeiroAtual = computed(() => this.pronto(this.financeiro(), this.podeVerFinanceiro())?.dados);
  protected readonly indicadores = computed(() => {
    const venda = this.pronto(this.vendas(), this.podeVerVendas());
    const compra = this.pronto(this.compras(), this.podeVerCompras());
    const financeiro = this.pronto(this.financeiro(), this.podeVerFinanceiro());

    const cartoes: Indicador[] = [];
    if (this.podeVerVendas()) cartoes.push(
      { titulo: 'Faturamento', icone: 'cash-stack', valor: venda?.dados.faturamento ?? null, anterior: venda?.anterior?.faturamento },
      { titulo: 'Lucro bruto', icone: 'graph-up', valor: venda?.dados.lucroBruto ?? null, anterior: venda?.anterior?.lucroBruto },
      { titulo: 'Margem bruta', icone: 'percent', valor: venda?.dados.margemBruta ?? null, anterior: venda?.anterior?.margemBruta, margem: true },
    );
    if (this.podeVerCompras()) cartoes.push(
      { titulo: 'Total de compras', icone: 'box-seam', valor: compra?.dados.totalCompras ?? null, anterior: compra?.anterior?.totalCompras, despesa: true },
    );
    if (this.podeVerFinanceiro()) cartoes.push(
      { titulo: 'Contas a pagar', icone: 'dash-circle', valor: financeiro?.dados.contasAPagar.saldo ?? null,
        anterior: financeiro?.anterior?.contasAPagar.saldo, despesa: true },
      { titulo: 'Contas a receber', icone: 'plus-circle', valor: financeiro?.dados.contasAReceber.saldo ?? null,
        anterior: financeiro?.anterior?.contasAReceber.saldo },
    );
    return cartoes;
  });
  protected readonly carregandoPeriodo = computed(() =>
    [this.vendas(), this.compras(), this.financeiro()].some(consulta => consulta?.estado === 'carregando'));
  protected readonly falhas = computed(() => [
    this.podeVerVendas() && this.vendas()?.estado === 'erro' ? 'Vendas' : '',
    this.podeVerCompras() && this.compras()?.estado === 'erro' ? 'Compras' : '',
    this.podeVerFinanceiro() && this.financeiro()?.estado === 'erro' ? 'Financeiro' : '',
  ].filter(Boolean));
  protected readonly faturamentoGrafico = computed(() => {
    const dados = this.vendasAtuais();
    return dados ? graficoFaturamento(dados, this.diaria(), this.cores()) : null;
  });
  protected readonly comercialGrafico = computed(() =>
    graficoComprasVendas(this.vendasAtuais(), this.comprasAtuais(), this.diaria(), this.cores()));
  protected readonly pagarGrafico = computed(() => {
    const dados = this.financeiroAtual();
    return dados ? graficoFinanceiro(dados.contasAPagar, this.cores()) : null;
  });
  protected readonly receberGrafico = computed(() => {
    const dados = this.financeiroAtual();
    return dados ? graficoFinanceiro(dados.contasAReceber, this.cores()) : null;
  });
  protected readonly rankingGrafico = computed(() => {
    const dados = this.vendasAtuais();
    return dados ? graficoRanking(dados, this.cores()) : null;
  });

  constructor() {
    effect(limpar => {
      const periodo = this.periodo();
      const anterior = this.comparacao();
      this.atualizacao();
      const consultas = new Subscription();
      if (this.podeVerVendas()) consultas.add(this.consultar(this.vendas,
        this.compararDatas(periodo, anterior, valor => this.dashboard.vendas(valor))));
      else this.vendas.set(null);
      if (this.podeVerCompras()) consultas.add(this.consultar(this.compras,
        this.compararDatas(periodo, anterior, valor => this.dashboard.compras(valor))));
      else this.compras.set(null);
      if (this.podeVerFinanceiro()) consultas.add(this.consultar(this.financeiro,
        this.compararDatas(periodo, anterior, valor => this.dashboard.indicadoresFinanceiros(valor))));
      else this.financeiro.set(null);
      limpar(() => consultas.unsubscribe());
    });

    effect(limpar => {
      this.atualizacaoEstoque();
      const pagina = this.paginaEstoque();
      const consulta = this.consultar(this.estoque,
        this.podeVerEstoque() ? this.dashboard.produtosAbaixoDoMinimo(pagina) : null);
      limpar(() => consulta?.unsubscribe());
    });
  }

  protected formatarConfirmacao(valor: string): string {
    return dataConfirmacao.format(new Date(valor));
  }

  protected pesquisar(): void {
    if (!this.filtro.dataInicio || !this.filtro.dataFim || this.filtro.dataInicio > this.filtro.dataFim) {
      this.erroPeriodo.set('Informe um período válido. A data inicial deve ser anterior ou igual à final.');
      return;
    }
    this.erroPeriodo.set('');
    this.atalhoAplicado.set('personalizado');
    this.periodo.set({ ...this.filtro });
  }

  protected selecionarAtalho(atalho: AtalhoPeriodo): void {
    this.erroPeriodo.set('');
    this.atalho.set(atalho);
    if (atalho === 'personalizado') {
      this.filtro = { ...this.periodo() };
      return;
    }
    this.filtro = periodoPredefinido(atalho);
    this.atalhoAplicado.set(atalho);
    this.periodo.set(this.filtro);
  }

  protected dataCurta(valor: string): string {
    return valor.split('-').reverse().join('/');
  }

  protected variacao(indicador: Indicador): number | null {
    const { valor, anterior, margem } = indicador;
    if (valor === null || anterior == null || (!margem && anterior === 0)) return null;
    return margem ? valor - anterior : (valor - anterior) / Math.abs(anterior) * 100;
  }

  private compararDatas<T>(
    periodo: PeriodoDashboard, anterior: PeriodoDashboard | null,
    buscar: (p: PeriodoDashboard) => Observable<T>,
  ) {
    return forkJoin({
      dados: buscar(periodo),
      anterior: anterior ? buscar(anterior).pipe(catchError(() => of(null))) : of(null),
    });
  }

  private pronto<T>(consulta: Consulta<T> | null, permitido: boolean): T | undefined {
    return permitido && consulta?.estado === 'pronto' ? consulta.dados : undefined;
  }

  private misturar(cor: string, alternativa: string, peso: number): string {
    if (!/^#[0-9a-f]{6}$/i.test(cor)) return alternativa;
    const partes = [1, 3, 5].map(inicio =>
      Math.round(parseInt(cor.slice(inicio, inicio + 2), 16) * (1 - peso) +
        parseInt(alternativa.slice(inicio, inicio + 2), 16) * peso));
    return '#' + partes.map(canal => canal.toString(16).padStart(2, '0')).join('');
  }

  protected recarregar(): void {
    this.atualizacao.update(valor => valor + 1);
  }

  protected recarregarEstoque(): void {
    this.atualizacaoEstoque.update(valor => valor + 1);
  }

  private consultar<T>(destino: WritableSignal<Consulta<T> | null>, consulta: Observable<T> | null) {
    if (!consulta) {
      destino.set(null);
      return;
    }
    destino.set({ estado: 'carregando' });
    return consulta.subscribe({
      next: dados => destino.set({ estado: 'pronto', dados }),
      error: () => destino.set({ estado: 'erro' }),
    });
  }
}
