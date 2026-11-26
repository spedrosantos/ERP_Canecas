import { CurrencyPipe, DatePipe, registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, LOCALE_ID, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Observable, Subscription, catchError, finalize, forkJoin, of, switchMap } from 'rxjs';
import { Moeda } from '../../shared/directives/moeda';
import {
  Baixa, FiltroTitulos, FinanceiroService, FormaPagamento, IndicadorFinanceiro, SituacaoTitulo,
  TipoTitulo, Titulo, TituloDetalhe, TitulosPaginados,
} from './financeiro.service';

registerLocaleData(localePt, 'pt-BR');

type Acao = { tipo: 'baixa' } | { tipo: 'estorno'; baixa: Baixa };

@Component({
  selector: 'app-financeiro',
  imports: [CurrencyPipe, DatePipe, FormsModule, Moeda],
  providers: [{ provide: LOCALE_ID, useValue: 'pt-BR' }],
  templateUrl: './financeiro.html',
  styleUrl: './financeiro.scss',
})
export class Financeiro {
  private readonly api = inject(FinanceiroService);
  private readonly destroyRef = inject(DestroyRef);
  private consulta?: Subscription;

  protected readonly tipo: TipoTitulo = inject(ActivatedRoute).snapshot.data['tipo'];
  protected readonly pagar = this.tipo === 'PAGAR';
  protected readonly tituloPagina = this.pagar ? 'Contas a Pagar' : 'Contas a Receber';
  protected readonly contraparte = this.pagar ? 'Fornecedor' : 'Cliente';
  protected readonly rotuloBaixa = this.pagar ? 'Registrar pagamento' : 'Registrar recebimento';
  protected readonly situacoes: Record<SituacaoTitulo, string> = {
    PENDENTE: 'Pendente', PARCIAL: 'Parcial', QUITADO: 'Quitado', VENCIDO: 'Vencido', CANCELADO: 'Cancelado',
  };
  protected readonly opcoesSituacao = Object.keys(this.situacoes) as SituacaoTitulo[];
  protected readonly formas: Record<FormaPagamento, string> = {
    PIX: 'Pix', DINHEIRO: 'Dinheiro', CARTAO: 'Cartão', BOLETO: 'Boleto', TRANSFERENCIA: 'Transferência',
  };
  protected readonly opcoesForma = Object.keys(this.formas) as FormaPagamento[];

  protected filtro = { codigo: '', situacao: '' as SituacaoTitulo | '', inicio: '', fim: '' };
  private filtroAplicado: FiltroTitulos = { tipo: this.tipo };

  protected readonly lista = signal<TitulosPaginados | null>(null);
  protected readonly indicadores = signal<{ aberto: number; vencido: number; quitado: number } | null>(null);
  protected readonly detalhe = signal<TituloDetalhe | null>(null);
  protected readonly carregando = signal(false);
  protected readonly abrindo = signal(false);
  protected readonly salvando = signal(false);
  protected readonly erroLista = signal('');
  protected readonly erroDetalhe = signal('');
  protected readonly erroAcao = signal('');
  protected readonly acao = signal<Acao | null>(null);
  protected formulario = this.formularioVazio();

  constructor() {
    this.carregar();
  }

  protected pesquisar(): void {
    const { codigo, situacao, inicio, fim } = this.filtro;
    const identificador = codigo.trim();
    if (identificador && (!/^\d{1,19}$/.test(identificador) ||
        BigInt(identificador) < 1n || BigInt(identificador) > 9223372036854775807n)) {
      this.erroLista.set('Informe um código válido.');
      return;
    }
    if (inicio && fim && inicio > fim) {
      this.erroLista.set('O vencimento inicial não pode ser posterior ao final.');
      return;
    }

    this.filtroAplicado = {
      tipo: this.tipo,
      ...(identificador ? this.pagar ? { fornecedorId: identificador } : { clienteId: identificador } : {}),
      ...(situacao ? { situacao } : {}),
      ...(inicio ? { vencimentoInicio: inicio } : {}),
      ...(fim ? { vencimentoFim: fim } : {}),
    };
    this.carregar();
  }

  protected limparFiltros(): void {
    this.filtro = { codigo: '', situacao: '', inicio: '', fim: '' };
    this.pesquisar();
  }

  protected carregar(pagina = 1): void {
    this.consulta?.unsubscribe();
    this.carregando.set(true);
    this.erroLista.set('');
    this.indicadores.set(null);

    const filtro = this.filtroAplicado;
    const consolidado = this.api.indicadores(filtro);
    const quitados = !filtro.situacao ? this.api.indicadores({ ...filtro, situacao: 'QUITADO' }) : of(null);
    const painel = forkJoin({ consolidado, quitados }).pipe(catchError(() => of(null)));

    this.consulta = forkJoin({
      lista: this.api.listar(filtro, pagina).pipe(
        switchMap(dados => pagina > this.totalPaginas(dados)
          ? this.api.listar(filtro, this.totalPaginas(dados)) : of(dados)),
      ),
      painel,
    }).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.carregando.set(false))).subscribe({
      next: ({ lista, painel }) => {
        this.lista.set(lista);
        if (!painel) {
          this.indicadores.set(null);
          return;
        }
        const principal = this.indicadorDoTipo(painel.consolidado);
        this.indicadores.set({
          aberto: principal.saldo,
          vencido: principal.saldoVencido,
          quitado: painel.quitados ? this.indicadorDoTipo(painel.quitados).valorBaixado
            : filtro.situacao === 'QUITADO' ? principal.valorBaixado : 0,
        });
      },
      error: erro => {
        this.lista.set(null);
        this.indicadores.set(null);
        this.erroLista.set(this.mensagem(erro, 'Não foi possível consultar os títulos.'));
      },
    });
  }

  private indicadorDoTipo(painel: {
    contasAPagar: IndicadorFinanceiro; contasAReceber: IndicadorFinanceiro;
  }): IndicadorFinanceiro {
    return this.pagar ? painel.contasAPagar : painel.contasAReceber;
  }

  protected totalPaginas(lista: TitulosPaginados): number {
    return Math.max(1, Math.ceil(lista.total / lista.tamanhoPagina));
  }

  protected nome(titulo: Titulo): string {
    return this.pagar
      ? titulo.nomeFornecedor ?? (titulo.fornecedorId ? '#' + titulo.fornecedorId : '—')
      : titulo.nomeCliente ?? (titulo.clienteId ? '#' + titulo.clienteId : '—');
  }

  protected origem(titulo: Titulo): string {
    if (titulo.compraId !== null) return 'Compra #' + titulo.compraId;
    if (titulo.vendaId !== null) return 'Venda #' + titulo.vendaId;
    return '—';
  }

  protected abrir(id: number): void {
    if (this.abrindo()) return;
    this.abrindo.set(true);
    this.erroDetalhe.set('');
    this.api.obter(id).pipe(
      takeUntilDestroyed(this.destroyRef), finalize(() => this.abrindo.set(false)),
    ).subscribe({
      next: dados => this.detalhe.set(dados),
      error: erro => this.erroDetalhe.set(this.mensagem(erro, 'Não foi possível consultar o título.')),
    });
  }

  protected voltar(): void {
    this.erroDetalhe.set('');
    this.detalhe.set(null);
  }

  protected podeBaixar(titulo: Titulo): boolean {
    return titulo.saldo > 0 && titulo.situacao !== 'CANCELADO' && titulo.situacao !== 'QUITADO';
  }

  protected iniciarBaixa(dialogo: HTMLDialogElement): void {
    this.abrirAcao(dialogo, { tipo: 'baixa' });
  }

  protected iniciarEstorno(dialogo: HTMLDialogElement, baixa: Baixa): void {
    this.abrirAcao(dialogo, { tipo: 'estorno', baixa });
  }

  private abrirAcao(dialogo: HTMLDialogElement, acao: Acao): void {
    this.formulario = this.formularioVazio();
    this.erroAcao.set('');
    this.acao.set(acao);
    dialogo.showModal();
  }

  private formularioVazio() {
    return { valor: null as number | null, formaPagamento: '' as FormaPagamento | '', observacao: '', motivo: '' };
  }

  protected fechar(dialogo: HTMLDialogElement, evento?: Event): void {
    evento?.preventDefault();
    if (this.salvando()) return;
    dialogo.close();
    this.acao.set(null);
  }

  protected salvar(dialogo: HTMLDialogElement, titulo: Titulo): void {
    const acao = this.acao();
    if (!acao || this.salvando()) return;
    const { valor, formaPagamento, observacao, motivo } = this.formulario;

    let operacao: Observable<Baixa>;
    if (acao.tipo === 'estorno') {
      if (!motivo.trim() || motivo.length > 500) {
        this.erroAcao.set('Informe o motivo do estorno com até 500 caracteres.');
        return;
      }
      operacao = this.api.estornar(titulo.id, acao.baixa.id, motivo.trim());
    } else {
      if (valor === null || valor <= 0 || valor > titulo.saldo || !Number.isSafeInteger(Math.round(valor * 100))) {
        this.erroAcao.set('Informe um valor positivo de até ' +
          new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(titulo.saldo) + '.');
        return;
      }
      if (!formaPagamento) {
        this.erroAcao.set('Selecione a forma de pagamento.');
        return;
      }
      operacao = this.api.registrar(titulo, { valor, formaPagamento, observacao: observacao.trim() || null });
    }

    this.salvando.set(true);
    this.erroAcao.set('');
    operacao.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.salvando.set(false))).subscribe({
      next: () => {
        dialogo.close();
        this.acao.set(null);
        this.abrir(titulo.id);
        this.carregar(this.lista()?.pagina ?? 1);
      },
      error: erro => this.erroAcao.set(this.mensagem(erro, 'Não foi possível concluir a operação.')),
    });
  }

  private mensagem(erro: unknown, padrao: string): string {
    if (!(erro instanceof HttpErrorResponse)) return padrao;
    if (typeof erro.error?.mensagem === 'string') return erro.error.mensagem;
    if (erro.status === 403) return 'Você não tem acesso a esta operação.';
    if (erro.status === 404) return 'O registro solicitado não foi encontrado.';
    return padrao;
  }
}
