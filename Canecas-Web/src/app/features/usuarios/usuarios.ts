import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, computed, DestroyRef, effect, ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, Observable } from 'rxjs';

import { FEATURES, FeatureService } from '../../core/services/feature.service';
import { Usuario } from './usuario';
import { UsuariosService } from './usuarios.service';

type FiltroSituacao = 'todos' | 'ativos' | 'inativos';

function normalizarBusca(valor: string): string {
  return valor.trim().normalize('NFD').replace(/\p{M}/gu, '').toLocaleLowerCase('pt-BR');
}

function mensagemDeErro(erro: unknown, padrao: string): string {
  if (!(erro instanceof HttpErrorResponse)) {
    return padrao;
  }

  switch (erro.status) {
    case 403:
      return 'Você não tem permissão para realizar esta operação.';
    case 404:
      return 'Usuário não encontrado.';
    case 409: {
      const mensagem: unknown = erro.error?.mensagem;
      return typeof mensagem === 'string' ? mensagem : 'A alteração não pôde ser concluída.';
    }
    case 0:
      return 'Não foi possível conectar ao serviço. Tente novamente.';
    default:
      return padrao;
  }
}

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './usuarios.html',
  styleUrl: './usuarios.scss',
})
export class Usuarios {
  private readonly api = inject(UsuariosService);
  private readonly features = inject(FeatureService);
  private readonly detector = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder).nonNullable;
  private readonly usuarios = signal<Usuario[]>([]);
  private readonly editor = viewChild<ElementRef<HTMLDialogElement>>('editor');
  private readonly confirmacao = viewChild<ElementRef<HTMLDialogElement>>('confirmacao');

  protected readonly podeCadastrar = computed(() => this.features.tem(FEATURES.cadastrarUsuario));
  protected readonly podeEditar = computed(() => this.features.tem(FEATURES.editarUsuario));
  protected readonly podeAlterarSenha = computed(() => this.features.tem(FEATURES.alterarSenhaUsuario));

  protected readonly busca = this.fb.control('');
  protected readonly situacao = this.fb.control<FiltroSituacao>('ativos');
  private readonly buscaAtual = toSignal(this.busca.valueChanges, { initialValue: this.busca.value });
  private readonly situacaoAtual = toSignal(this.situacao.valueChanges, {
    initialValue: this.situacao.value,
  });

  protected readonly filtrados = computed(() => {
    const busca = normalizarBusca(this.buscaAtual());
    const situacao = this.situacaoAtual();

    return this.usuarios().filter((usuario) =>
      (situacao === 'todos' || usuario.ativo === (situacao === 'ativos')) &&
      (String(usuario.id).includes(busca) || normalizarBusca(usuario.nome).includes(busca)),
    );
  });

  protected readonly carregando = signal(false);
  protected readonly salvando = signal(false);
  protected readonly erroLista = signal('');
  protected readonly erroModal = signal('');
  protected readonly aviso = signal('');
  protected readonly usuarioEmEdicao = signal<Usuario | null>(null);
  protected readonly somenteSenha = signal(false);
  protected readonly exibirSenha = computed(() => !this.usuarioEmEdicao() || this.podeAlterarSenha());
  private readonly podeSalvarEditor = computed(() => {
    if (this.somenteSenha()) {
      return this.podeAlterarSenha();
    }
    return this.usuarioEmEdicao() ? this.podeEditar() : this.podeCadastrar();
  });
  protected readonly alvoSituacao = signal<Usuario | null>(null);
  protected readonly senhaVisivel = signal(false);
  protected readonly confirmacaoVisivel = signal(false);
  protected readonly formulario = this.fb.group(
    {
      nome: ['', [Validators.required, Validators.maxLength(150), Validators.pattern(/\S/)]],
      senha: ['', [Validators.minLength(8), Validators.pattern(/\S/)]],
      confirmarSenha: ['', Validators.minLength(8)],
    },
    {
      validators: (grupo) => {
        if (!this.usuarioEmEdicao()) {
          return null;
        }

        const senha: string = grupo.get('senha')?.value;
        const confirmarSenha: string = grupo.get('confirmarSenha')?.value;
        if (Boolean(senha) !== Boolean(confirmarSenha)) {
          return { senhasIncompletas: true };
        }

        return senha === confirmarSenha ? null : { senhasDiferentes: true };
      },
    },
  );

  constructor() {
    this.carregar();
    effect(() => {
      const editor = this.editor()?.nativeElement;
      const confirmacao = this.confirmacao()?.nativeElement;
      if (!this.podeSalvarEditor() && editor?.open) {
        editor.close();
      }
      if (!this.exibirSenha()) {
        this.formulario.controls.senha.reset();
        this.formulario.controls.confirmarSenha.reset();
      }
      if (!this.podeEditar() && confirmacao?.open) {
        confirmacao.close();
      }
    });
  }

  protected carregar(): void {
    if (this.carregando()) {
      return;
    }

    this.erroLista.set('');
    this.carregando.set(true);
    this.api.listar()
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.carregando.set(false)))
      .subscribe({
        next: (usuarios) => this.usuarios.set(usuarios),
        error: (erro: unknown) =>
          this.erroLista.set(mensagemDeErro(erro, 'Não foi possível carregar os usuários.')),
      });
  }

  protected abrirEditor(dialogo: HTMLDialogElement, usuario: Usuario | null = null, somenteSenha = false): void {
    if (!(somenteSenha ? this.podeAlterarSenha() : usuario ? this.podeEditar() : this.podeCadastrar())) {
      return;
    }

    if (usuario && !this.validarCodigo(usuario.id)) {
      return;
    }

    this.usuarioEmEdicao.set(usuario);
    this.somenteSenha.set(somenteSenha);
    this.erroModal.set('');
    this.senhaVisivel.set(false);
    this.confirmacaoVisivel.set(false);
    const { nome, senha } = this.formulario.controls;
    if (somenteSenha) {
      nome.disable();
    } else {
      nome.enable();
    }
    if (usuario && !somenteSenha) {
      senha.removeValidators(Validators.required);
    } else {
      senha.addValidators(Validators.required);
    }
    this.formulario.reset({ nome: usuario?.nome ?? '', senha: '' });
    this.detector.detectChanges();
    dialogo.showModal();
  }

  protected salvarEditor(dialogo: HTMLDialogElement): void {
    const usuario = this.usuarioEmEdicao();
    if (this.salvando() || !this.podeSalvarEditor()) {
      return;
    }

    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) {
      return;
    }

    const { nome, senha } = this.formulario.getRawValue();
    const dados = { nome: nome.trim() };
    if (usuario) {
      if (this.somenteSenha()) {
        this.salvar(dialogo, this.api.alterarSenha(usuario.id, senha), 'Senha alterada.');
      } else {
        const edicao = senha && this.podeAlterarSenha() ? { ...dados, senha } : dados;
        this.salvar(dialogo, this.api.editar(usuario.id, edicao), 'Usuário atualizado.');
      }
    } else {
      this.salvar(dialogo, this.api.cadastrar({ ...dados, senha }), 'Usuário cadastrado.');
    }
  }

  protected abrirConfirmacao(dialogo: HTMLDialogElement, usuario: Usuario): void {
    if (!this.podeEditar() || !this.validarCodigo(usuario.id)) {
      return;
    }

    this.erroModal.set('');
    this.alvoSituacao.set(usuario);
    this.detector.detectChanges();
    dialogo.showModal();
  }

  protected confirmarSituacao(dialogo: HTMLDialogElement): void {
    const usuario = this.alvoSituacao();
    if (!usuario || this.salvando() || !this.podeEditar()) {
      return;
    }

    const ativo = !usuario.ativo;
    this.salvar(
      dialogo,
      this.api.alterarSituacao(usuario.id, ativo),
      ativo ? 'Usuário ativado.' : 'Usuário desativado.',
    );
  }

  protected cancelarDialogo(evento: Event): void {
    if (this.salvando()) {
      evento.preventDefault();
    }
  }

  private validarCodigo(id: number): boolean {
    if (Number.isSafeInteger(id) && id > 0) {
      return true;
    }

    this.aviso.set('O código deste usuário não pode ser processado com segurança no navegador.');
    return false;
  }

  private salvar(dialogo: HTMLDialogElement, operacao: Observable<Usuario>, mensagem: string): void {
    this.erroModal.set('');
    this.aviso.set('');
    this.salvando.set(true);
    operacao
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.salvando.set(false)))
      .subscribe({
        next: (usuario) => {
          this.usuarios.update((atuais) =>
            [...atuais.filter((atual) => atual.id !== usuario.id), usuario].sort((a, b) => a.id - b.id),
          );
          dialogo.close();
          this.aviso.set(mensagem);
        },
        error: (erro: unknown) =>
          this.erroModal.set(mensagemDeErro(erro, 'Não foi possível salvar a alteração.')),
      });
  }
}
