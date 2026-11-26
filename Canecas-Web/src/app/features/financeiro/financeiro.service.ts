import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MENSAGEM_SUCESSO } from '../../core/interceptors/notificacoes.interceptor';

export type TipoTitulo = 'PAGAR' | 'RECEBER';
export type SituacaoTitulo = 'PENDENTE' | 'PARCIAL' | 'QUITADO' | 'VENCIDO' | 'CANCELADO';
export type FormaPagamento = 'PIX' | 'DINHEIRO' | 'CARTAO' | 'BOLETO' | 'TRANSFERENCIA';

export interface FiltroTitulos {
  tipo: TipoTitulo;
  situacao?: SituacaoTitulo;
  fornecedorId?: string;
  clienteId?: string;
  vencimentoInicio?: string;
  vencimentoFim?: string;
}

export interface Titulo {
  id: number;
  tipo: TipoTitulo;
  compraId: number | null;
  vendaId: number | null;
  fornecedorId: number | null;
  nomeFornecedor: string | null;
  clienteId: number | null;
  nomeCliente: string | null;
  valor: number;
  valorBaixado: number;
  saldo: number;
  situacao: SituacaoTitulo;
  vencimento: string;
  usuarioId: number;
  criadoEm: string;
  canceladoEm: string | null;
}

export interface Baixa {
  id: number;
  tituloId: number;
  valor: number;
  formaPagamento: FormaPagamento;
  usuarioId: number;
  observacao: string | null;
  criadoEm: string;
  estornadoEm: string | null;
  usuarioEstornoId: number | null;
  motivoEstorno: string | null;
}

export interface TitulosPaginados {
  itens: Titulo[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}

export interface TituloDetalhe {
  titulo: Titulo;
  baixas: Baixa[];
}

export interface IndicadorFinanceiro {
  totalTitulos: number;
  valorTotal: number;
  valorBaixado: number;
  saldo: number;
  saldoVencido: number;
}

export interface RegistrarBaixa {
  valor: number;
  formaPagamento: FormaPagamento;
  observacao: string | null;
}

@Injectable({ providedIn: 'root' })
export class FinanceiroService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/financeiro';

  listar(filtro: FiltroTitulos, pagina: number) {
    return this.http.get<TitulosPaginados>(this.base + '/titulos', {
      params: { ...filtro, pagina, tamanhoPagina: 20 },
    });
  }

  obter(id: number) {
    return this.http.get<TituloDetalhe>(this.base + '/titulos/' + id);
  }

  indicadores(filtro: FiltroTitulos) {
    return this.http.get<{ contasAPagar: IndicadorFinanceiro; contasAReceber: IndicadorFinanceiro }>(
      this.base + '/indicadores', { params: { ...filtro } },
    );
  }

  registrar(titulo: Titulo, dados: RegistrarBaixa) {
    const mensagem = titulo.tipo === 'PAGAR' ? 'Pagamento registrado com sucesso.' : 'Recebimento registrado com sucesso.';
    const context = new HttpContext().set(MENSAGEM_SUCESSO, mensagem);
    return this.http.post<Baixa>(this.base + '/titulos/' + titulo.id + '/baixas', dados, { context });
  }

  estornar(id: number, baixaId: number, motivo: string) {
    const context = new HttpContext().set(MENSAGEM_SUCESSO, 'Baixa estornada com sucesso.');
    return this.http.post<Baixa>(this.base + '/titulos/' + id + '/baixas/' + baixaId + '/estornar', { motivo }, { context });
  }
}
