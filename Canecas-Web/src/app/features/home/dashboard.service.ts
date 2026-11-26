import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { EstoqueService } from '../estoque/estoque.service';
import { FinanceiroService } from '../financeiro/financeiro.service';

export interface PeriodoDashboard {
  dataInicio: string;
  dataFim: string;
}

export interface MesDashboard {
  mes: string;
  valor: number;
}

export interface ResumoVendas {
  faturamento: number;
  vendasConfirmadas: number;
  lucroBruto: number | null;
  margemBruta: number | null;
  evolucao: MesDashboard[];
  produtosMaisVendidos: { produtoId: number; nomeProduto: string | null; quantidade: number }[];
  ultimasVendas: { id: number; clienteId: number; nomeCliente: string | null; confirmadoEm: string; valor: number }[];
}

export interface ResumoCompras {
  totalCompras: number;
  evolucao: MesDashboard[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly financeiro = inject(FinanceiroService);
  private readonly estoque = inject(EstoqueService);

  vendas(periodo: PeriodoDashboard) {
    return this.http.get<ResumoVendas>('/api/vendas/pedidos/dashboard', { params: { ...periodo } });
  }

  compras(periodo: PeriodoDashboard) {
    return this.http.get<ResumoCompras>('/api/compras/pedidos/dashboard', { params: { ...periodo } });
  }

  indicadoresFinanceiros(periodo: PeriodoDashboard) {
    return this.financeiro.indicadores({
      vencimentoInicio: periodo.dataInicio, vencimentoFim: periodo.dataFim,
    });
  }

  produtosAbaixoDoMinimo(pagina: number) {
    return this.estoque.listarSaldos({ abaixoDoMinimo: true, pagina, tamanhoPagina: 5 });
  }
}
