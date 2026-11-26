import { CurrencyPipe, DatePipe, DecimalPipe, registerLocaleData } from '@angular/common';
import pt from '@angular/common/locales/pt';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, ElementRef, LOCALE_ID, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Observable, Subscription, finalize } from 'rxjs';
import { NotificacoesService } from '../../core/services/notificacoes.service';
import { Mascara } from '../../shared/directives/mascara';
import { dataAtualParaFormulario } from '../../shared/formatacao/data-local';
import { Moeda } from '../../shared/directives/moeda';
import { BuscaCadastro, CadastroAtivo, URL_BUSCA_CADASTRO } from '../../shared/components/busca-cadastro/busca-cadastro';
import {
  ComprasService, ItemCompra, Pagina, Pedido, PedidoDetalhe, SituacaoPedido, URL_COMPRAS
} from './compras.service';

registerLocaleData(pt);

const FORMATO_QUANTIDADE = /^\d{1,12}(\.\d{1,3})?$/;
const FORMATO_CUSTO = /^\d{1,11}(\.\d{1,4})?$/;

interface ItemEdicao {
  produtoId: number;
  quantidade: number | null;
  custoUnitario: number | null;
  nome?: string | null;
  unidadeMedida?: string | null;
}

interface Edicao {
  id?: number;
  criadoEm?: string;
  fornecedorId: number | null;
  nomeFornecedor?: string;
  observacao: string;
  itens: ItemEdicao[];
}

@Component({
  selector: 'app-compras',
  imports: [FormsModule, DatePipe, CurrencyPipe, DecimalPipe, BuscaCadastro, Moeda, Mascara],
  providers: [
    { provide: LOCALE_ID, useValue: 'pt-BR' },
    { provide: URL_BUSCA_CADASTRO, useValue: URL_COMPRAS }
  ],
  templateUrl: './compras.html',
  styleUrls: ['../../shared/styles/pedidos.scss', '../../shared/styles/pedidos-editor.scss']
})
export class Compras {
  private readonly notificacoes = inject(NotificacoesService);
  private readonly api = inject(ComprasService);
  private readonly destroyRef = inject(DestroyRef);
  private requisicaoLista?: Subscription;
  private filtroAplicado: Record<string, string | number> = {};

  protected readonly lista = signal<Pagina<Pedido> | null>(null);
  protected readonly detalhe = signal<PedidoDetalhe | null>(null);
  protected readonly produtoSelecionado = signal<CadastroAtivo | null>(null);
  protected readonly edicao = signal<Edicao | null>(null);
  protected readonly carregando = signal(false);
  protected readonly ocupado = signal(false);
  protected readonly erroLista = signal('');
  protected readonly erroAcao = signal('');
  protected readonly aviso = signal('');
  private readonly confirmacao = viewChild<ElementRef<HTMLDialogElement>>('confirmacao');
  protected readonly acaoPendente = signal<'receber' | 'cancelar' | null>(null);
  protected vencimento = '';
  protected readonly situacoes: Record<SituacaoPedido, string> = {
    PENDENTE: 'Pendente', RECEBIDO: 'Recebido', CANCELADO: 'Cancelado'
  };

  protected filtro = { fornecedorId: null as number | null, situacao: '', dataInicio: '', dataFim: '' };

  constructor() {
    this.carregar();
  }

  protected dialogoAberto(): boolean {
    return this.acaoPendente() !== null;
  }

  protected totalPaginas(dados: Pagina<Pedido>): number {
    return Math.max(1, Math.ceil(dados.total / dados.tamanhoPagina));
  }

  protected pesquisar() {
    const { fornecedorId, situacao, dataInicio, dataFim } = this.filtro;
    if (fornecedorId !== null && (!Number.isSafeInteger(fornecedorId) || fornecedorId < 1)) {
      this.erroValidacao(this.erroLista, 'Informe um código de fornecedor válido.');
      return;
    }
    if (dataInicio && dataFim && dataInicio > dataFim) {
      this.erroValidacao(this.erroLista, 'A data inicial não pode ser posterior à data final.');
      return;
    }

    this.filtroAplicado = {
      ...(fornecedorId !== null ? { fornecedorId } : {}),
      ...(situacao ? { situacao } : {}),
      ...(dataInicio ? { dataInicio } : {}),
      ...(dataFim ? { dataFim } : {})
    };
    this.carregar();
  }

  protected carregar(pagina = 1) {
    this.requisicaoLista?.unsubscribe();
    this.erroLista.set('');
    this.carregando.set(true);
    this.requisicaoLista = this.api.listar({ ...this.filtroAplicado, pagina, tamanhoPagina: 20 })
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.carregando.set(false)))
      .subscribe({
        next: dados => {
          if (!dados.itens.length && pagina > 1) {
            this.carregar(Math.max(1, Math.min(pagina - 1, Math.ceil(dados.total / dados.tamanhoPagina))));
            return;
          }
          this.lista.set(dados);
        },
        error: erro => this.erroLista.set(this.mensagem(erro, 'Não foi possível listar os pedidos.'))
      });
  }

  protected abrir(id: number) {
    this.aviso.set('');
    this.executar(this.api.obter(id), 'Não foi possível consultar o pedido.');
  }

  protected novo() {
    this.produtoSelecionado.set(null);
    this.aviso.set('');
    this.erroAcao.set('');
    this.edicao.set({ fornecedorId: null, observacao: '', itens: [] });
  }

  protected editar(detalhe: PedidoDetalhe) {
    this.produtoSelecionado.set(null);
    this.aviso.set('');
    this.erroAcao.set('');
    this.edicao.set({
      id: detalhe.pedido.id,
      criadoEm: detalhe.pedido.criadoEm,
      fornecedorId: detalhe.pedido.fornecedorId,
      nomeFornecedor: detalhe.nomeFornecedor ?? undefined,
      observacao: detalhe.pedido.observacao ?? '',
      itens: detalhe.itens.map(item => ({
        produtoId: item.produtoId, quantidade: item.quantidade, custoUnitario: item.custoUnitario,
        nome: item.nomeProduto, unidadeMedida: item.unidadeMedida
      }))
    });
  }

  protected voltar() {
    this.edicao.set(null);
    this.erroAcao.set('');
  }

  protected selecionarFornecedor(edicao: Edicao, fornecedor: CadastroAtivo) {
    edicao.fornecedorId = fornecedor.id;
    edicao.nomeFornecedor = fornecedor.nome;
  }

  protected adicionarProduto(edicao: Edicao) {
    const produto = this.produtoSelecionado();
    if (!produto) return;

    this.produtoSelecionado.set(null);
    if (edicao.itens.some(item => item.produtoId === produto.id)) {
      this.erroValidacao(this.erroAcao, 'Este produto já está no pedido.');
      return;
    }
    this.erroAcao.set('');
    edicao.itens.push({ produtoId: produto.id, nome: produto.nome, unidadeMedida: produto.unidadeMedida, quantidade: 1, custoUnitario: null });
  }

  protected subtotal(item: ItemEdicao): number {
    return this.emReais(this.valorItem(item));
  }

  protected resumir(itens: ItemEdicao[]) {
    let quantidade = 0;
    let valor = 0n;
    for (const item of itens) {
      quantidade += item.quantidade ?? 0;
      valor += this.valorItem(item);
    }
    return { quantidade, valor: this.emReais(valor) };
  }

  private valorItem(item: ItemEdicao): bigint {
    const quantidade = item.quantidade ?? 0;
    const custo = item.custoUnitario ?? 0;
    if (!FORMATO_QUANTIDADE.test(String(quantidade)) ||
        !FORMATO_CUSTO.test(String(custo))) return 0n;

    // Escala de sete casas: três da quantidade e quatro do custo.
    return BigInt(Math.round(quantidade * 1000)) * BigInt(Math.round(custo * 10000));
  }

  private emReais(valor: bigint): number {
    return Number((valor + 50000n) / 100000n) / 100;
  }

  protected salvar(edicao: Edicao) {
    if (this.ocupado()) return;
    if (edicao.fornecedorId === null || !edicao.itens.length) {
      this.erroValidacao(this.erroAcao, 'Selecione um fornecedor e adicione pelo menos um produto.');
      return;
    }
    const itens: ItemCompra[] = [];
    for (const { produtoId, quantidade, custoUnitario } of edicao.itens) {
      if (quantidade === null || quantidade <= 0 || !FORMATO_QUANTIDADE.test(String(quantidade)) ||
          custoUnitario === null || !FORMATO_CUSTO.test(String(custoUnitario))) {
        this.erroValidacao(this.erroAcao, `Revise o produto #${produtoId}. Informe quantidade positiva com até três casas decimais e custo com até quatro.`);
        return;
      }
      itens.push({ produtoId, quantidade, custoUnitario });
    }
    this.executar(this.api.salvar({
      fornecedorId: edicao.fornecedorId, observacao: edicao.observacao, itens
    }, edicao.id), 'Não foi possível salvar o pedido.', 'Pedido salvo.');
  }

  protected solicitarAcao(acao: 'receber' | 'cancelar', dialogo: HTMLDialogElement): void {
    this.erroAcao.set('');
    this.vencimento = acao === 'receber' ? dataAtualParaFormulario() : '';
    this.acaoPendente.set(acao);
    dialogo.showModal();
  }

  protected fecharConfirmacao(evento?: Event): void {
    evento?.preventDefault();
    if (this.ocupado()) return;
    this.erroAcao.set('');
    this.encerrarConfirmacao();
  }

  protected aplicarAcao(id: number, valorTotal: number): void {
    const acao = this.acaoPendente();
    if (!acao || this.ocupado()) return;
    if (acao === 'receber' && valorTotal > 0 && !this.vencimento) {
      this.erroAcao.set('Informe o vencimento da conta a pagar.');
      return;
    }

    this.executar(this.api.alterarSituacao(id, acao, this.vencimento || undefined),
      'Não foi possível atualizar a situação do pedido.',
      acao === 'receber' ? 'Pedido recebido e estoque atualizado.' : 'Pedido cancelado.');
  }

  private encerrarConfirmacao(): void {
    const dialogo = this.confirmacao()?.nativeElement;
    if (dialogo?.open) dialogo.close();
    this.acaoPendente.set(null);
  }

  private executar(consulta: Observable<PedidoDetalhe>, falha: string, sucesso?: string) {
    if (this.ocupado()) return;
    this.ocupado.set(true);
    this.erroAcao.set('');
    consulta.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.ocupado.set(false)))
      .subscribe({
        next: detalhe => {
          this.encerrarConfirmacao();
          this.detalhe.set(detalhe);
          this.edicao.set(null);
          if (sucesso) {
            this.aviso.set(sucesso);
            this.carregar(this.lista()?.pagina ?? 1);
          }
        },
        error: erro => this.erroAcao.set(this.mensagem(erro, falha))
      });
  }

  private erroValidacao(destino: typeof this.erroAcao, mensagem: string): void {
    destino.set(mensagem);
    this.notificacoes.erro(mensagem);
  }

  private mensagem(erro: HttpErrorResponse, padrao: string): string {
    if (typeof erro.error?.mensagem === 'string') return erro.error.mensagem;
    if (erro.status === 403) return 'Você não tem acesso a esta operação.';
    if (erro.status === 404) return 'Pedido não encontrado.';
    return padrao;
  }
}
