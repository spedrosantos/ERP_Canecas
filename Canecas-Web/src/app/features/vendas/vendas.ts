import { CurrencyPipe, DatePipe, DecimalPipe, registerLocaleData } from '@angular/common';
import pt from '@angular/common/locales/pt';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, ElementRef, LOCALE_ID, inject, signal, viewChild, WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Observable, Subscription, finalize, of, switchMap } from 'rxjs';
import { NotificacoesService } from '../../core/services/notificacoes.service';
import { Mascara } from '../../shared/directives/mascara';
import { Moeda } from '../../shared/directives/moeda';
import { BuscaCadastro, CadastroAtivo, URL_BUSCA_CADASTRO } from '../../shared/components/busca-cadastro/busca-cadastro';
import {
  VendasService, ItemVenda, Pagina, Pedido, PedidoDetalhe, SituacaoPedido, URL_VENDAS
} from './vendas.service';

registerLocaleData(pt);

type AcaoVenda = 'confirmar' | 'cancelar';

const FORMATO_REAIS = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const FORMATO_QUANTIDADE = /^\d{1,12}(\.\d{1,3})?$/;
const FORMATO_PRECO = /^\d{1,11}(\.\d{1,4})?$/;

interface ItemEdicao {
  produtoId: number;
  quantidade: number | null;
  precoUnitario: number | null;
  nome?: string | null;
  unidadeMedida?: string | null;
  saldo: WritableSignal<number | null | undefined>;
}

interface Edicao {
  id?: number;
  criadoEm?: string;
  clienteId: number | null;
  nomeCliente?: string;
  observacao: string;
  itens: ItemEdicao[];
}

@Component({
  selector: 'app-vendas',
  imports: [FormsModule, DatePipe, CurrencyPipe, DecimalPipe, BuscaCadastro, Moeda, Mascara],
  providers: [
    { provide: LOCALE_ID, useValue: 'pt-BR' },
    { provide: URL_BUSCA_CADASTRO, useValue: URL_VENDAS }
  ],
  templateUrl: './vendas.html',
  styleUrls: ['../../shared/styles/pedidos.scss', '../../shared/styles/pedidos-editor.scss', './vendas.scss']
})
export class Vendas {
  private readonly notificacoes = inject(NotificacoesService);
  private readonly api = inject(VendasService);
  private readonly destroyRef = inject(DestroyRef);
  private requisicaoLista?: Subscription;
  private readonly consultasSaldo = new Map<number, Subscription>();
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
  protected readonly acaoPendente = signal<AcaoVenda | null>(null);
  private readonly confirmacao = viewChild<ElementRef<HTMLDialogElement>>('confirmacao');
  protected readonly situacoes: Record<SituacaoPedido, string> = {
    PENDENTE: 'Pendente', CONFIRMADO: 'Confirmado', CANCELADO: 'Cancelado'
  };

  protected filtro = { clienteId: null as number | null, situacao: '', dataInicio: '', dataFim: '' };

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
    const { clienteId, situacao, dataInicio, dataFim } = this.filtro;
    if (clienteId !== null && (!Number.isSafeInteger(clienteId) || clienteId < 1)) {
      this.erroValidacao(this.erroLista, 'Informe um código de cliente válido.');
      return;
    }
    if (dataInicio && dataFim && dataInicio > dataFim) {
      this.erroValidacao(this.erroLista, 'A data inicial não pode ser posterior à data final.');
      return;
    }

    this.filtroAplicado = {
      ...(clienteId !== null ? { clienteId } : {}),
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
    const consultar = (numero: number) =>
      this.api.listar({ ...this.filtroAplicado, pagina: numero, tamanhoPagina: 20 });
    this.requisicaoLista = consultar(pagina)
      .pipe(
        switchMap(dados => !dados.itens.length && pagina > 1
          ? consultar(Math.min(pagina - 1, this.totalPaginas(dados)))
          : of(dados)),
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.carregando.set(false)),
      )
      .subscribe({
        next: dados => this.lista.set(dados),
        error: erro => {
          this.lista.set(null);
          this.erroLista.set(this.mensagem(erro, 'Não foi possível listar as vendas.'));
        }
      });
  }

  protected abrir(id: number) {
    this.aviso.set('');
    this.executar(this.api.obter(id), 'Não foi possível consultar a venda.');
  }

  protected novo() {
    this.limparConsultasSaldo();
    this.produtoSelecionado.set(null);
    this.aviso.set('');
    this.erroAcao.set('');
    this.edicao.set({ clienteId: null, observacao: '', itens: [] });
  }

  protected editar(detalhe: PedidoDetalhe) {
    this.limparConsultasSaldo();
    this.produtoSelecionado.set(null);
    this.aviso.set('');
    this.erroAcao.set('');
    const itens = detalhe.itens.map(item => ({
      produtoId: item.produtoId, quantidade: item.quantidade, precoUnitario: item.precoUnitario,
      nome: item.nomeProduto, unidadeMedida: item.unidadeMedida,
      saldo: signal<number | null | undefined>(undefined),
    }));
    this.edicao.set({
      id: detalhe.pedido.id,
      criadoEm: detalhe.pedido.criadoEm,
      clienteId: detalhe.pedido.clienteId,
      nomeCliente: detalhe.nomeCliente ?? undefined,
      observacao: detalhe.pedido.observacao ?? '',
      itens,
    });
    for (const item of itens) this.carregarSaldo(item);
  }

  protected voltar() {
    this.limparConsultasSaldo();
    this.edicao.set(null);
    this.erroAcao.set('');
  }

  protected selecionarCliente(edicao: Edicao, cliente: CadastroAtivo) {
    edicao.clienteId = cliente.id;
    edicao.nomeCliente = cliente.nome;
  }

  protected adicionarProduto(edicao: Edicao) {
    const produto = this.produtoSelecionado();
    if (!produto) return;

    this.produtoSelecionado.set(null);
    if (edicao.itens.some(item => item.produtoId === produto.id)) {
      this.erroValidacao(this.erroAcao, 'Este produto já está na venda.');
      return;
    }
    this.erroAcao.set('');
    const item: ItemEdicao = {
      produtoId: produto.id, nome: produto.nome, unidadeMedida: produto.unidadeMedida,
      quantidade: 1, precoUnitario: null, saldo: signal(undefined),
    };
    edicao.itens.push(item);
    this.carregarSaldo(item);
  }

  protected removerProduto(edicao: Edicao, indice: number): void {
    const [item] = edicao.itens.splice(indice, 1);
    this.consultasSaldo.get(item.produtoId)?.unsubscribe();
    this.consultasSaldo.delete(item.produtoId);
  }

  private carregarSaldo(item: ItemEdicao): void {
    const requisicao = this.api.consultarDisponibilidade(item.produtoId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: dados => item.saldo.set(dados.quantidadeDisponivel),
        error: () => item.saldo.set(null),
      });
    this.consultasSaldo.set(item.produtoId, requisicao);
  }

  private limparConsultasSaldo(): void {
    for (const consulta of this.consultasSaldo.values()) consulta.unsubscribe();
    this.consultasSaldo.clear();
  }

  protected subtotal(item: Pick<ItemEdicao, 'quantidade' | 'precoUnitario'>): string {
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

  private valorItem(item: Pick<ItemEdicao, 'quantidade' | 'precoUnitario'>): bigint {
    const quantidade = item.quantidade ?? 0;
    const preco = item.precoUnitario ?? 0;
    if (!FORMATO_QUANTIDADE.test(String(quantidade)) ||
        !FORMATO_PRECO.test(String(preco))) return 0n;

    return this.escalar(quantidade, 3) * this.escalar(preco, 4);
  }

  private escalar(valor: number, casas: number): bigint {
    const [inteiro, fracao = ''] = String(valor).split('.');
    return BigInt(inteiro + fracao.padEnd(casas, '0'));
  }

  private emReais(valor: bigint): string {
    const centavos = (valor + 50000n) / 100000n;
    const fracao = String(centavos % 100n).padStart(2, '0');
    return FORMATO_REAIS.formatToParts(centavos / 100n)
      .map(parte => parte.type === 'fraction' ? fracao : parte.value).join('');
  }

  protected salvar(edicao: Edicao) {
    if (this.ocupado()) return;
    if (edicao.clienteId === null || !edicao.itens.length) {
      this.erroValidacao(this.erroAcao, 'Selecione um cliente e adicione pelo menos um produto.');
      return;
    }
    const itens: ItemVenda[] = [];
    for (const { produtoId, quantidade, precoUnitario } of edicao.itens) {
      if (quantidade === null || quantidade <= 0 || !FORMATO_QUANTIDADE.test(String(quantidade)) ||
          precoUnitario === null || precoUnitario <= 0 || !FORMATO_PRECO.test(String(precoUnitario))) {
        this.erroValidacao(this.erroAcao, `Revise o produto #${produtoId}. Informe quantidade positiva com até três casas decimais e preço positivo com até quatro.`);
        return;
      }
      itens.push({ produtoId, quantidade, precoUnitario });
    }
    this.executar(this.api.salvar({
      clienteId: edicao.clienteId, observacao: edicao.observacao, itens
    }, edicao.id), 'Não foi possível salvar a venda.', 'Venda salva.');
  }

  protected solicitarAcao(acao: AcaoVenda, dialogo: HTMLDialogElement): void {
    this.erroAcao.set('');
    this.acaoPendente.set(acao);
    dialogo.showModal();
  }

  protected fecharConfirmacao(evento?: Event): void {
    evento?.preventDefault();
    if (this.ocupado()) return;
    this.erroAcao.set('');
    this.encerrarConfirmacao();
  }

  protected aplicarAcao(id: number): void {
    const acao = this.acaoPendente();
    if (!acao || this.ocupado()) return;

    this.executar(this.api.alterarSituacao(id, acao),
      acao === 'confirmar' ? 'Não foi possível confirmar a venda.' : 'Não foi possível cancelar a venda.',
      acao === 'confirmar' ? 'Venda confirmada e estoque atualizado.' : 'Venda cancelada.');
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
          this.limparConsultasSaldo();
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
    if (erro.error?.errors && typeof erro.error.errors === 'object') {
      const detalhes = Object.values(erro.error.errors).flat().filter(valor => typeof valor === 'string');
      if (detalhes.length) return detalhes.join(' ');
    }
    if (erro.status === 403) return 'Você não tem acesso a esta operação.';
    if (erro.status === 404) return 'Venda não encontrada.';
    return padrao;
  }
}
