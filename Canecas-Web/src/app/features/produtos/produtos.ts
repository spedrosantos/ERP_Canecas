import {
  Component, OnInit, inject, signal,
  ViewChild, ElementRef
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ProdutosService } from './produtos.service';
import {
  Produto,
  Categoria,
  UnidadeMedida,
  TipoProduto,
  SalvarProduto
} from './produto';

@Component({
  selector: 'app-produtos',
  imports: [ReactiveFormsModule],
  templateUrl: './produtos.html',
  styleUrl: './produtos.scss'
})
export class Produtos implements OnInit {
  private readonly service = inject(ProdutosService);
  private readonly fb = inject(FormBuilder);

  readonly produtos = signal<Produto[]>([]);
  readonly categorias = signal<Categoria[]>([]);
  readonly unidades = signal<UnidadeMedida[]>([]);

  readonly carregando = signal(false);
  readonly salvando = signal(false);
  readonly modalAberto = signal(false);
  readonly erro = signal('');
  readonly editandoId = signal<number | null>(null);

  readonly pagina = signal(1);
  readonly total = signal(0);
  readonly tamanhoPagina = 20;

  @ViewChild('editor')
  private editor!: ElementRef<HTMLDialogElement>;

  readonly tipos = [
    { valor: 'ACABADO', nome: 'Produto acabado' },
    { valor: 'MATERIA_PRIMA', nome: 'Matéria-prima' },
    { valor: 'REVENDA', nome: 'Revenda' }
  ];

  readonly filtros = this.fb.nonNullable.group({
    busca: '',
    categoriaId: '',
    tipo: '',
    ativo: ''
  });

  readonly formulario = this.fb.nonNullable.group({
    categoriaId: [0, [Validators.min(1)]],
    unidadeMedidaId: [0, [Validators.min(1)]],
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    descricao: ['', Validators.maxLength(500)],
    tipo: ['ACABADO' as TipoProduto, Validators.required]
  });

  ngOnInit(): void {
    this.carregarAuxiliares();
    this.listar();
  }

  carregarAuxiliares(): void {
    this.service.listarCategorias().subscribe({
      next: categorias => this.categorias.set(categorias),
      error: () => this.erro.set('Erro ao carregar categorias.')
    });

    this.service.listarUnidades().subscribe({
      next: unidades => this.unidades.set(unidades),
      error: () => this.erro.set('Erro ao carregar unidades.')
    });
  }

  listar(): void {
    this.carregando.set(true);
    this.erro.set('');

    const filtro = this.filtros.getRawValue();

    this.service.listar({
      busca: filtro.busca || undefined,
      categoriaId: Number(filtro.categoriaId) || undefined,
      tipo: (filtro.tipo as TipoProduto) || undefined,
      ativo: filtro.ativo === ''
        ? undefined
        : filtro.ativo === 'true',
      pagina: this.pagina(),
      tamanhoPagina: this.tamanhoPagina
    }).subscribe({
      next: resposta => {
        this.produtos.set(resposta.itens);
        this.total.set(resposta.total);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set('Erro ao carregar produtos.');
        this.carregando.set(false);
      }
    });
  }

  pesquisar(): void {
    this.pagina.set(1);
    this.listar();
  }

  mudarPagina(pagina: number): void {
    if (
      pagina < 1 ||
      pagina > Math.ceil(this.total() / this.tamanhoPagina)
    ) return;

    this.pagina.set(pagina);
    this.listar();
  }

  novo(): void {
    this.editandoId.set(null);
    this.erro.set('');

    this.formulario.reset({
      categoriaId: 0,
      unidadeMedidaId: 0,
      nome: '',
      descricao: '',
      tipo: 'ACABADO'
    });

    this.modalAberto.set(true);
    this.editor.nativeElement.showModal();
  }

  editar(produto: Produto): void {
    this.editandoId.set(produto.id);
    this.erro.set('');

    this.formulario.patchValue({
      ...produto,
      descricao: produto.descricao ?? ''
    });

    this.modalAberto.set(true);
    this.editor.nativeElement.showModal();
  }

  fecharModal(): void {
    if (this.salvando()) return;

    this.editor.nativeElement.close();
    this.modalAberto.set(false);
    this.erro.set('');
  }

  @ViewChild('confirmacao')
  private confirmacao!: ElementRef<HTMLDialogElement>;

  readonly produtoSelecionado = signal<Produto | null>(null);
  readonly erroConfirmacao = signal('');

  cancelarDialogo(event: Event): void {
    event.preventDefault();
    this.fecharModal();
  }

  salvar(): void {
    if (this.salvando()) return;

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const valores = this.formulario.getRawValue();

    const dto: SalvarProduto = {
      ...valores,
      categoriaId: Number(valores.categoriaId),
      unidadeMedidaId: Number(valores.unidadeMedidaId),
      nome: valores.nome.trim(),
      descricao: valores.descricao.trim() || null
    };

    const id = this.editandoId();

    this.salvando.set(true);
    this.erro.set('');

    const requisicao = id === null
      ? this.service.cadastrar(dto)
      : this.service.atualizar(id, dto);

    requisicao.subscribe({
      next: () => {
        this.salvando.set(false);
        this.fecharModal();
        this.listar();
      },
      error: resposta => {
        this.salvando.set(false);

        this.erro.set(
          resposta.error?.mensagem || 'Erro ao salvar produto.'
        );
      }
    });
  }

  alterarSituacao(produto: Produto): void {
    this.produtoSelecionado.set(produto);
    this.erroConfirmacao.set('');
    this.confirmacao.nativeElement.showModal();
  }

  confirmarSituacao(): void {
    const produto = this.produtoSelecionado();

    if (!produto || this.salvando()) return;

    this.salvando.set(true);
    this.erroConfirmacao.set('');

    this.service.alterarSituacao(
      produto.id,
      !produto.ativo
    ).subscribe({
      next: () => {
        this.salvando.set(false);
        this.fecharConfirmacao();
        this.listar();
      },
      error: resposta => {
        this.salvando.set(false);
        this.erroConfirmacao.set(
          resposta.error?.mensagem ||
          'Erro ao alterar situação.'
        );
      }
    });
  }

  fecharConfirmacao(): void {
    if (this.salvando()) return;

    this.confirmacao.nativeElement.close();
    this.produtoSelecionado.set(null);
    this.erroConfirmacao.set('');
  }

  cancelarConfirmacao(event: Event): void {
    event.preventDefault();
    this.fecharConfirmacao();
  }
}