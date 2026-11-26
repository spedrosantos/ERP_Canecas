import { CurrencyPipe, DatePipe, DecimalPipe, registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { Component, LOCALE_ID, WritableSignal, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable, Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { FEATURES, FeatureService } from '../../core/services/feature.service';
import { dataAtualParaFormulario } from '../../shared/formatacao/data-local';
import { ResultadoPaginado, SaldoEstoque } from '../estoque/estoque.model';
import { IndicadorFinanceiro } from '../financeiro/financeiro.service';
import { DashboardService, PeriodoDashboard, ResumoCompras, ResumoVendas } from './dashboard.service';

registerLocaleData(localePt, 'pt-BR');

const dataConfirmacao = new Intl.DateTimeFormat('pt-BR', {
  timeZone: 'America/Sao_Paulo', day: '2-digit', month: '2-digit', year: 'numeric',
});

type Consulta<T> = { estado: 'carregando' | 'erro' } | { estado: 'pronto'; dados: T };
type ConsolidadoFinanceiro = { contasAPagar: IndicadorFinanceiro; contasAReceber: IndicadorFinanceiro };

@Component({
  selector: 'app-home',
  imports: [CurrencyPipe, DatePipe, DecimalPipe, FormsModule, RouterLink],
  providers: [{ provide: LOCALE_ID, useValue: 'pt-BR' }],
  templateUrl: './home.html',
  styleUrls: ['./home.scss', './home-grafico.scss'],
})
export class Home {
  protected readonly auth = inject(AuthService);
  private readonly features = inject(FeatureService);
  private readonly dashboard = inject(DashboardService);

  protected readonly permissoesProntas = this.features.prontas;
  protected readonly podeVerVendas = computed(() => this.features.permiteRota(FEATURES.vendas));
  protected readonly podeVerCompras = computed(() => this.features.permiteRota(FEATURES.compras));
  protected readonly podeVerFinanceiro = computed(() => this.features.permiteRota(FEATURES.financeiro));
  protected readonly podeVerEstoque = computed(() => this.features.permiteRota(FEATURES.estoque));
  protected readonly temIndicadores = computed(() =>
    this.podeVerVendas() || this.podeVerCompras() || this.podeVerFinanceiro() || this.podeVerEstoque(),
  );

  protected filtro = this.mesAtual();
  protected readonly periodo = signal(this.filtro);
  protected readonly erroPeriodo = signal('');
  private readonly atualizacao = signal(0);
  private readonly atualizacaoEstoque = signal(0);
  protected readonly paginaEstoque = signal(1);
  protected readonly vendas = signal<Consulta<ResumoVendas> | null>(null);
  protected readonly compras = signal<Consulta<ResumoCompras> | null>(null);
  protected readonly financeiro = signal<Consulta<ConsolidadoFinanceiro> | null>(null);
  protected readonly estoque = signal<Consulta<ResultadoPaginado<SaldoEstoque>> | null>(null);

  protected readonly grafico = computed(() => {
    const vendas = this.podeVerVendas() ? this.vendas() : null;
    const compras = this.podeVerCompras() ? this.compras() : null;
    const meses = new Map<string, { mes: string; vendas?: number; compras?: number }>();
    for (const item of vendas?.estado === 'pronto' ? vendas.dados.evolucao : []) {
      meses.set(item.mes, { mes: item.mes, vendas: item.valor });
    }
    for (const item of compras?.estado === 'pronto' ? compras.dados.evolucao : []) {
      meses.set(item.mes, { ...meses.get(item.mes), mes: item.mes, compras: item.valor });
    }
    const pontos = [...meses.values()].sort((a, b) => a.mes.localeCompare(b.mes));
    const maior = pontos.reduce((maximo, ponto) => Math.max(maximo, ponto.vendas ?? 0, ponto.compras ?? 0), 1);
    return { pontos, maior };
  });

  constructor() {
    effect(limpar => {
      const periodo = this.periodo();
      this.atualizacao();
      const consultas = new Subscription();
      consultas.add(this.consultar(this.vendas, this.podeVerVendas() ? this.dashboard.vendas(periodo) : null));
      consultas.add(this.consultar(this.compras, this.podeVerCompras() ? this.dashboard.compras(periodo) : null));
      consultas.add(this.consultar(this.financeiro,
        this.podeVerFinanceiro() ? this.dashboard.indicadoresFinanceiros(periodo) : null));
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
    this.periodo.set({ ...this.filtro });
  }

  protected voltarAoMesAtual(): void {
    this.filtro = this.mesAtual();
    this.pesquisar();
  }

  protected recarregar(): void {
    this.atualizacao.update(valor => valor + 1);
  }

  protected recarregarEstoque(): void {
    this.atualizacaoEstoque.update(valor => valor + 1);
  }

  private mesAtual(): PeriodoDashboard {
    const hoje = dataAtualParaFormulario();
    const [ano, mes] = hoje.split('-').map(Number);
    const ultimoDia = new Date(Date.UTC(ano, mes, 0)).getUTCDate();
    return { dataInicio: hoje.slice(0, 8) + '01', dataFim: hoje.slice(0, 8) + ultimoDia };
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
