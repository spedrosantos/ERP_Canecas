import { DestroyRef, Injectable, inject, signal } from '@angular/core';

type TipoNotificacao = 'sucesso' | 'erro';

@Injectable({ providedIn: 'root' })
export class NotificacoesService {
  private readonly fila = signal<{ id: number; tipo: TipoNotificacao; mensagem: string }[]>([]);
  private readonly prazos = new Map<number, ReturnType<typeof setTimeout>>();
  private proximoId = 0;

  readonly itens = this.fila.asReadonly();

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      for (const prazo of this.prazos.values()) clearTimeout(prazo);
    });
  }

  sucesso(mensagem: string): void {
    this.adicionar('sucesso', mensagem);
  }

  erro(mensagem: string): void {
    this.adicionar('erro', mensagem);
  }

  fechar(id: number): void {
    clearTimeout(this.prazos.get(id));
    this.prazos.delete(id);
    this.fila.update(itens => itens.filter(item => item.id !== id));
  }

  private adicionar(tipo: TipoNotificacao, mensagem: string): void {
    if (this.fila().some(item => item.tipo === tipo && item.mensagem === mensagem)) return;

    const maisAntigo = this.fila()[0];
    if (this.fila().length >= 4) this.fechar(maisAntigo.id);

    const id = ++this.proximoId;
    this.fila.update(itens => [...itens, { id, tipo, mensagem }]);
    this.prazos.set(id, setTimeout(() => this.fechar(id), tipo === 'erro' ? 10000 : 5000));
  }
}
