import { Component, OnInit, inject, signal, computed, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { EstoqueService } from './estoque.service';
import {
  SaldoEstoque,
  MovimentacaoEstoque,
  TipoMovimentacao,
  OrigemMovimentacao
} from './estoque.model';

@Component({
  selector: 'app-estoque',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './estoque.html',
  styleUrl: './estoque.scss'
})
export class Estoque implements OnInit {
  private readonly service = inject(EstoqueService);
  private readonly fb = inject(FormBuilder).nonNullable;

  readonly aba = signal<'saldos' | 'movimentacoes'>('saldos');

  readonly saldos = signal<SaldoEstoque[]>([]);
  readonly movimentacoes = signal<MovimentacaoEstoque[]>([]);

  readonly carregando = signal(false);
  readonly erro = signal('');
  readonly aviso = signal('');

  readonly pagina = signal(1);
  readonly total = signal(0);
  readonly tamanhoPagina = 20;

  readonly ultimaPagina = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.tamanhoPagina))
  );

  readonly filtrosSaldos = this.fb.group({
    busca: '',
    abaixoDoMinimo: ''
  });

  readonly filtrosMovimentacoes = this.fb.group({
    produtoId: '',
    tipo: '',
    origem: '',
    dataInicio: '',
    dataFim: ''
  });

  @ViewChild('editorAjuste')
  private editorAjuste!: ElementRef<HTMLDialogElement>;

  @ViewChild('editorMinimo')
  private editorMinimo!: ElementRef<HTMLDialogElement>;

  @ViewChild('editorEstorno')
  private editorEstorno!: ElementRef<HTMLDialogElement>;

  readonly movimentacaoSelecionada =
    signal<MovimentacaoEstoque | null>(null);

  readonly saldoSelecionado = signal<SaldoEstoque | null>(null);
  readonly salvando = signal(false);
  readonly erroEditor = signal('');

  readonly formularioAjuste = this.fb.group({
    tipo: ['ENTRADA' as TipoMovimentacao, Validators.required],
    quantidade: [1, [Validators.required, Validators.min(0.001)]],
    custoUnitario: [0, [Validators.min(0)]],
    justificativa: ['', [
      Validators.required,
      Validators.maxLength(500)
    ]]
  });

  abrirAjuste(saldo: SaldoEstoque): void {
    this.saldoSelecionado.set(saldo);
    this.erroEditor.set('');

    this.formularioAjuste.reset({
      tipo: 'ENTRADA',
      quantidade: 1,
      custoUnitario: saldo.custoMedio,
      justificativa: ''
    });

    this.editorAjuste.nativeElement.showModal();
  }

  fecharAjuste(): void {
    if (this.salvando()) return;

    this.editorAjuste.nativeElement.close();
    this.saldoSelecionado.set(null);
    this.erroEditor.set('');
  }

  cancelarAjuste(event: Event): void {
    event.preventDefault();
    this.fecharAjuste();
  }

  salvarAjuste(): void {
    const saldo = this.saldoSelecionado();

    if (!saldo || this.salvando()) return;

    if (this.formularioAjuste.invalid) {
      this.formularioAjuste.markAllAsTouched();
      return;
    }

    const valores = this.formularioAjuste.getRawValue();

    if (!valores.justificativa.trim()) {
      this.erroEditor.set('Informe a justificativa.');
      return;
    }

    this.salvando.set(true);
    this.erroEditor.set('');

    this.service.ajustar({
      produtoId: saldo.produtoId,
      tipo: valores.tipo,
      quantidade: valores.quantidade,
      custoUnitario: valores.tipo === 'ENTRADA'
        ? valores.custoUnitario
        : null,
      justificativa: valores.justificativa.trim()
    }).subscribe({
      next: () => {
        this.salvando.set(false);
        this.fecharAjuste();
        this.aviso.set('Estoque ajustado com sucesso.');
        this.carregar(this.pagina());
      },
      error: resposta => {
        this.salvando.set(false);
        this.erroEditor.set(
          resposta.error?.mensagem ||
          'Erro ao ajustar estoque.'
        );
      }
    });
  }

  abrirMinimo(saldo: SaldoEstoque): void {
    this.saldoSelecionado.set(saldo);
    this.erroEditor.set('');

    this.formularioMinimo.reset({
      estoqueMinimo: saldo.estoqueMinimo
    });

    this.editorMinimo.nativeElement.showModal();
  }

  fecharMinimo(): void {
    if (this.salvando()) return;

    this.editorMinimo.nativeElement.close();
    this.saldoSelecionado.set(null);
    this.erroEditor.set('');
  }

  cancelarMinimo(event: Event): void {
    event.preventDefault();
    this.fecharMinimo();
  }

  salvarMinimo(): void {
    const saldo = this.saldoSelecionado();

    if (!saldo || this.salvando()) return;

    if (this.formularioMinimo.invalid) {
      this.formularioMinimo.markAllAsTouched();
      return;
    }

    const { estoqueMinimo } =
      this.formularioMinimo.getRawValue();

    this.salvando.set(true);
    this.erroEditor.set('');

    this.service.configurarMinimo(
      saldo.produtoId,
      estoqueMinimo
    ).subscribe({
      next: () => {
        this.salvando.set(false);
        this.fecharMinimo();
        this.aviso.set('Estoque mínimo atualizado.');
        this.carregar(this.pagina());
      },
      error: resposta => {
        this.salvando.set(false);
        this.erroEditor.set(
          resposta.error?.mensagem ||
          'Erro ao configurar estoque mínimo.'
        );
      }
    });
  }

  readonly formularioMinimo = this.fb.group({
    estoqueMinimo: [0, [
      Validators.required,
      Validators.min(0),
      Validators.max(999999999999.999)
    ]]
  });

  readonly formularioEstorno = this.fb.group({
    justificativa: ['', [
      Validators.required,
      Validators.maxLength(500)
    ]]
  });

  abrirEstorno(movimentacao: MovimentacaoEstoque): void {
    this.movimentacaoSelecionada.set(movimentacao);
    this.erroEditor.set('');

    this.formularioEstorno.reset({
      justificativa: ''
    });

    this.editorEstorno.nativeElement.showModal();
  }

  fecharEstorno(): void {
    if (this.salvando()) return;

    this.editorEstorno.nativeElement.close();
    this.movimentacaoSelecionada.set(null);
    this.erroEditor.set('');
  }

  cancelarEstorno(event: Event): void {
    event.preventDefault();
    this.fecharEstorno();
  }

  salvarEstorno(): void {
    const movimentacao = this.movimentacaoSelecionada();

    if (!movimentacao || this.salvando()) return;

    const justificativa =
      this.formularioEstorno.controls.justificativa.value.trim();

    if (this.formularioEstorno.invalid || !justificativa) {
      this.formularioEstorno.markAllAsTouched();
      this.erroEditor.set('Informe uma justificativa válida.');
      return;
    }

    this.salvando.set(true);
    this.erroEditor.set('');

    this.service.estornar(
      movimentacao.id,
      justificativa
    ).subscribe({
      next: () => {
        this.salvando.set(false);
        this.fecharEstorno();
        this.aviso.set('Movimentação estornada com sucesso.');
        this.carregar(1);
      },
      error: resposta => {
        this.salvando.set(false);
        this.erroEditor.set(
          resposta.error?.mensagem ||
          'Erro ao estornar movimentação.'
        );
      }
    });
  }

  ngOnInit(): void {
    this.carregar();
  }

  trocarAba(aba: 'saldos' | 'movimentacoes'): void {
    if (this.aba() === aba) return;

    this.aba.set(aba);
    this.aviso.set('');
    this.carregar(1);
  }

  carregar(pagina = 1): void {
    this.pagina.set(pagina);
    this.carregando.set(true);
    this.erro.set('');

    if (this.aba() === 'saldos') {
      this.carregarSaldos();
    } else {
      this.carregarMovimentacoes();
    }
  }

  private carregarSaldos(): void {
    const filtro = this.filtrosSaldos.getRawValue();

    this.service.listarSaldos({
      busca: filtro.busca.trim() || undefined,
      abaixoDoMinimo: filtro.abaixoDoMinimo === ''
        ? undefined
        : filtro.abaixoDoMinimo === 'true',
      pagina: this.pagina(),
      tamanhoPagina: this.tamanhoPagina
    }).subscribe({
      next: resposta => {
        this.saldos.set(resposta.itens);
        this.total.set(resposta.total);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set('Erro ao consultar saldos.');
        this.carregando.set(false);
      }
    });
  }

  private carregarMovimentacoes(): void {
    const filtro = this.filtrosMovimentacoes.getRawValue();

    this.service.listarMovimentacoes({
      produtoId: Number(filtro.produtoId) || undefined,
      tipo: (filtro.tipo as TipoMovimentacao) || undefined,
      origem: (filtro.origem as OrigemMovimentacao) || undefined,
      dataInicio: filtro.dataInicio || undefined,
      dataFim: filtro.dataFim || undefined,
      pagina: this.pagina(),
      tamanhoPagina: this.tamanhoPagina
    }).subscribe({
      next: resposta => {
        this.movimentacoes.set(resposta.itens);
        this.total.set(resposta.total);
        this.carregando.set(false);
      },
      error: resposta => {
        this.erro.set(
          resposta.error?.mensagem ||
          'Erro ao consultar movimentações.'
        );
        this.carregando.set(false);
      }
    });
  }

  mudarPagina(pagina: number): void {
    if (pagina < 1 || pagina > this.ultimaPagina())
      return;

    this.carregar(pagina);
  }
}
