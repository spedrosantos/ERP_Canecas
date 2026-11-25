import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';

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
