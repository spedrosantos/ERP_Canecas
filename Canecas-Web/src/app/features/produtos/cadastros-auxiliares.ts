import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, ElementRef, inject, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, Subscription } from 'rxjs';
import { CadastroAuxiliar, Categoria, UnidadeMedida } from './produto';
import { ProdutosService } from './produtos.service';

type Registro = Categoria | UnidadeMedida;

type Tela =
  | { tipo: 'lista' }
  | { tipo: 'editor'; registro?: Registro }
  | { tipo: 'confirmacao'; registro: Registro };

@Component({
  selector: 'app-cadastros-auxiliares',
  imports: [ReactiveFormsModule],
  templateUrl: './cadastros-auxiliares.html',
  styleUrl: './cadastros-auxiliares.scss'
})
export class CadastrosAuxiliares {
  private readonly service = inject(ProdutosService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');
  private consulta?: Subscription;

  readonly alterado = output<void>();
  protected readonly cadastro = signal<CadastroAuxiliar>('categorias');
  protected readonly tela = signal<Tela>({ tipo: 'lista' });
  protected readonly registros = signal<Registro[]>([]);
  protected readonly carregando = signal(false);
  protected readonly salvando = signal(false);
  protected readonly erro = signal('');

  protected readonly formulario = inject(FormBuilder).nonNullable.group({
    descricao: ['', [Validators.required, Validators.maxLength(100)]],
    sigla: ['', Validators.maxLength(10)]
  });

  abrir(cadastro: CadastroAuxiliar): void {
    this.cadastro.set(cadastro);
    this.tela.set({ tipo: 'lista' });
    this.registros.set([]);
    this.dialogo().nativeElement.showModal();
    this.carregar();
  }

  protected carregar(): void {
    this.consulta?.unsubscribe();
    this.erro.set('');
    this.carregando.set(true);

    const consulta: Observable<Registro[]> = this.cadastro() === 'categorias'
      ? this.service.listarCategorias(false)
      : this.service.listarUnidades(false);

    this.consulta = consulta.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: registros => {
        this.registros.set(registros);
        this.carregando.set(false);
      },
      error: (erro: unknown) => {
        this.exibirErro(erro, 'Não foi possível carregar os registros.');
        this.carregando.set(false);
      }
    });
  }

  protected editar(registro?: Registro): void {
    this.erro.set('');
    this.formulario.reset({
      descricao: registro ? this.descricao(registro) : '',
      sigla: registro && 'sigla' in registro ? registro.sigla : ''
    });
    this.formulario.controls.sigla.setValidators(
      this.cadastro() === 'categorias' ? [] : [Validators.required, Validators.maxLength(10)]
    );
    this.formulario.controls.sigla.updateValueAndValidity();
    this.tela.set({ tipo: 'editor', registro });
  }

  protected descricao(registro: Registro): string {
    return 'nome' in registro ? registro.nome : registro.descricao;
  }

  protected pedirConfirmacao(registro: Registro): void {
    this.erro.set('');
    this.tela.set({ tipo: 'confirmacao', registro });
  }

  protected salvar(): void {
    const tela = this.tela();
    if (this.salvando() || tela.tipo !== 'editor') return;

    const { descricao, sigla } = this.formulario.getRawValue();
    const dados = { descricao: descricao.trim(), sigla: sigla.trim() };
    this.formulario.setValue(dados);
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    this.executar(this.service.salvarCadastro(
      this.cadastro(), tela.registro?.id ?? null, dados.descricao, dados.sigla
    ));
  }

  protected confirmar(): void {
    const tela = this.tela();
    if (this.salvando() || tela.tipo !== 'confirmacao') return;

    this.executar(this.service.alterarSituacaoCadastro(
      this.cadastro(), tela.registro.id, !tela.registro.ativo
    ));
  }

  protected voltar(event?: Event): void {
    event?.preventDefault();
    if (this.salvando()) return;

    if (this.tela().tipo === 'lista') {
      this.consulta?.unsubscribe();
      this.dialogo().nativeElement.close();
    } else {
      this.erro.set('');
      this.tela.set({ tipo: 'lista' });
    }
  }

  private executar(requisicao: Observable<unknown>): void {
    this.salvando.set(true);
    this.erro.set('');
    requisicao.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.salvando.set(false);
        this.tela.set({ tipo: 'lista' });
        this.alterado.emit();
        this.carregar();
      },
      error: (erro: unknown) => {
        this.exibirErro(erro, 'Não foi possível concluir a operação.');
        this.salvando.set(false);
      }
    });
  }

  private exibirErro(erro: unknown, padrao: string): void {
    if (!(erro instanceof HttpErrorResponse)) {
      this.erro.set(padrao);
      return;
    }

    const resposta = erro.error;
    const validacoes = Object.values(resposta?.errors ?? {}).flat()
      .filter((mensagem): mensagem is string => typeof mensagem === 'string')
      .join(' ');

    this.erro.set(resposta?.mensagem || validacoes || padrao);
  }
}
