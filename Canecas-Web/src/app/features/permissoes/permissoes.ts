import { NgTemplateOutlet } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { FEATURE_GRUPOS } from '../../core/config/feature-grupos';

import { FEATURES, FeatureService } from '../../core/services/feature.service';
import {
  AtualizarPermissoesEmLote,
  Feature,
  PermissoesEmLote,
  PermissoesService,
} from './permissoes.service';

interface NoFeature extends Feature {
  filhos: NoFeature[];
}

function montarArvore(features: Feature[]): NoFeature[] {
  const nos = new Map(features.map((feature): [number, NoFeature] => [
    feature.id, { ...feature, filhos: [] },
  ]));
  const raizes: NoFeature[] = [];

  for (const no of nos.values()) {
    const pai = no.idFeaturePai === null ? undefined : nos.get(no.idFeaturePai);
    const visitados = new Set([no.id]);
    let ancestral = pai;
    while (ancestral && !visitados.has(ancestral.id)) {
      visitados.add(ancestral.id);
      ancestral = ancestral.idFeaturePai === null ? undefined : nos.get(ancestral.idFeaturePai);
    }

    if (pai && !ancestral) {
      pai.filhos.push(no);
    } else {
      raizes.push(no);
    }
  }

  return raizes;
}

function normalizar(valor: string): string {
  return valor.trim().normalize('NFD').replace(/\p{M}/gu, '').toLocaleLowerCase('pt-BR');
}

function filtrarArvore(nos: NoFeature[], termo: string): NoFeature[] {
  return nos.flatMap(no => {
    if (normalizar(no.nome).includes(termo) || normalizar(no.codigo).includes(termo)) {
      return [no];
    }

    const filhos = filtrarArvore(no.filhos, termo);
    return filhos.length ? [{ ...no, filhos }] : [];
  });
}

function mensagemErro(erro: unknown, padrao: string): string {
  if (!(erro instanceof HttpErrorResponse)) {
    return padrao;
  }

  if (erro.status === 403) {
    return 'Você não tem autorização para realizar esta operação.';
  }
  if (erro.status === 404) {
    return 'Um dos usuários selecionados não foi encontrado.';
  }
  const mensagem: unknown = erro.error?.mensagem;
  return typeof mensagem === 'string' ? mensagem : padrao;
}

@Component({
  selector: 'app-permissoes',
  standalone: true,
  imports: [NgTemplateOutlet],
  templateUrl: './permissoes.html',
  styleUrl: './permissoes.scss',
})
export class Permissoes {
  private readonly api = inject(PermissoesService);
  private readonly acesso = inject(FeatureService);
  private readonly detector = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dados = signal<PermissoesEmLote | null>(null);

  protected readonly termo = signal('');
  protected readonly termoFeature = signal('');
  private readonly gruposAbertos = signal(new Map<number, boolean>());
  private readonly gruposDaBusca = signal(new Map<number, boolean>());
  protected readonly usuariosSelecionados = signal(new Set<number>());
  private readonly alteracoes = signal(new Map<number, boolean>());
  protected readonly operacao = signal<AtualizarPermissoesEmLote | null>(null);
  protected readonly carregando = signal(false);
  protected readonly aplicando = signal(false);
  protected readonly erroConsulta = signal('');
  protected readonly erroOperacao = signal('');
  protected readonly aviso = signal('');
  protected readonly usuariosFiltrados = computed(() => {
    const termo = normalizar(this.termo());
    return this.dados()?.usuarios.filter(usuario => usuario.ativo &&
      (String(usuario.id).includes(termo) || normalizar(usuario.nome).includes(termo)),
    ) ?? [];
  });
  private readonly arvore = computed(() => montarArvore(this.dados()?.features ?? []));

  protected readonly featuresAgrupadas = computed(() => {
    const arvore = this.arvore();
    const termo = normalizar(this.termoFeature());

    const grupos = FEATURE_GRUPOS.map(grupo => ({
      nome: grupo.nome,
      modulos: grupo.modulos.flatMap(modulo => {
        const no = arvore.find(n => n.codigo === modulo.codigo);

        if (!no) {
          return [];
        }

        const permissoes = no.filhos.length
          ? no.filhos
          : [no];

        return [{
          id: no.id,
          nome: modulo.nome,
          permissoes
        }];
      })
    }));

    const outros = arvore.filter(no =>
      !FEATURE_GRUPOS.some(grupo =>
        grupo.modulos.some(modulo => modulo.codigo === no.codigo)
      )
    );

    if (outros.length) {
      grupos.push({
        nome: 'Outros',
        modulos: outros.map(no => ({
          id: no.id,
          nome: no.nome,
          permissoes: no.filhos.length ? no.filhos : [no]
        }))
      });
    }

    return grupos.map(grupo => ({
      ...grupo,
      modulos: grupo.modulos.map(modulo => ({
        ...modulo,
        permissoes: !termo ||
          normalizar(grupo.nome).includes(termo) ||
          normalizar(modulo.nome).includes(termo)
          ? modulo.permissoes
          : filtrarArvore(modulo.permissoes, termo)
      })).filter(modulo => modulo.permissoes.length > 0)
    })).filter(grupo => grupo.modulos.length > 0);
  });
  private readonly catalogo = computed(() => new Map(
    this.dados()?.features.map(feature => [feature.id, feature]),
  ));
  protected readonly gerenciaveis = computed(() => new Set(
    this.podeGerenciar() ? this.dados()?.featureIdsGerenciaveis.filter(Number.isSafeInteger) : [],
  ));
  private readonly atribuicoes = computed(() => {
    const contagem = new Map<number, number>();
    for (const usuario of this.dados()?.usuarios ?? []) {
      if (this.usuariosSelecionados().has(usuario.id)) {
        for (const id of usuario.featureIds) {
          contagem.set(id, (contagem.get(id) ?? 0) + 1);
        }
      }
    }
    return contagem;
  });
  private readonly alteracoesPendentes = computed(() => {
    const concederIds: number[] = [];
    const revogarIds: number[] = [];
    const quantidade = this.usuariosSelecionados().size;
    for (const [id, marcado] of this.alteracoes()) {
      if (!this.gerenciaveis().has(id)) {
        continue;
      }
      const atribuicoes = this.atribuicoes().get(id) ?? 0;
      if (marcado && atribuicoes < quantidade) {
        concederIds.push(id);
      } else if (!marcado && atribuicoes > 0) {
        revogarIds.push(id);
      }
    }
    return { concederIds, revogarIds };
  });
  protected readonly podeAplicar = computed(() => {
    const { concederIds, revogarIds } = this.alteracoesPendentes();
    return this.podeGerenciar() && !!this.usuariosSelecionados().size &&
      !!(concederIds.length || revogarIds.length) && !this.aplicando();
  });
  protected readonly podeGerenciar = computed(() => this.acesso.tem(FEATURES.permissoes));
  protected readonly operacaoAutorizada = computed(() => {
    const operacao = this.operacao();
    return this.podeGerenciar() && !!operacao &&
      [...operacao.concederIds, ...operacao.revogarIds].every(id => this.gerenciaveis().has(id));
  });
  protected readonly codigoValido = Number.isSafeInteger;

  constructor() {
    this.carregar();
    effect(() => {
      const permitidas = this.gerenciaveis();
      this.alteracoes.update(alteracoes => {
        const autorizadas = new Map([...alteracoes].filter(([id]) => permitidas.has(id)));
        return autorizadas.size === alteracoes.size ? alteracoes : autorizadas;
      });
    });
  }

  protected carregar(): void {
    if (this.carregando()) {
      return;
    }

    this.carregando.set(true);
    this.erroConsulta.set('');
    this.api.consultar()
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.carregando.set(false)))
      .subscribe({
        next: dados => this.dados.set(dados),
        error: (erro: unknown) => this.erroConsulta.set(
          mensagemErro(erro, 'Não foi possível carregar as permissões.'),
        ),
      });
  }

  protected selecionarUsuario(id: number, marcado: boolean): void {
    this.usuariosSelecionados.update(ids => this.atualizarSelecao(ids, id, marcado));
    this.alteracoes.set(new Map());
  }

  protected buscarFeatures(termo: string): void {
    this.termoFeature.set(termo);
    this.gruposDaBusca.set(new Map());
  }

  protected grupoAberto(id: number): boolean {
    if (this.termoFeature().trim()) {
      return this.gruposDaBusca().get(id) ?? true;
    }

    const raizes = this.arvore();
    return this.gruposAbertos().get(id) ?? (raizes.length === 1 && raizes[0].id === id);
  }

  protected alternarGrupo(id: number): void {
    const grupos = this.termoFeature().trim() ? this.gruposDaBusca : this.gruposAbertos;
    const abrir = !this.grupoAberto(id);
    grupos.update(atuais => new Map(atuais).set(id, abrir));
  }

  protected estadoFeature(id: number): boolean | 'misto' {
    const alteracao = this.alteracoes().get(id);
    if (alteracao !== undefined) {
      return alteracao;
    }

    const quantidade = this.atribuicoes().get(id) ?? 0;
    return quantidade === 0 ? false : quantidade === this.usuariosSelecionados().size ? true : 'misto';
  }

  protected selecionarFeature(no: Feature, marcado: boolean): void {
    const alteracoes = new Map(this.alteracoes());
    const permitidas = this.gerenciaveis();
    if (marcado) {
      let atual: Feature | undefined = no;
      while (atual && permitidas.has(atual.id)) {
        alteracoes.set(atual.id, true);
        atual = atual.idFeaturePai === null ? undefined : this.catalogo().get(atual.idFeaturePai);
      }
    } else {
      alteracoes.set(no.id, false);
    }
    this.alteracoes.set(alteracoes);
  }

  protected confirmar(dialogo: HTMLDialogElement): void {
    if (!this.podeAplicar()) {
      return;
    }

    this.erroOperacao.set('');
    this.operacao.set({
      usuarioIds: [...this.usuariosSelecionados()],
      ...this.alteracoesPendentes(),
    });
    this.detector.detectChanges();
    dialogo.showModal();
  }

  protected cancelar(evento: Event): void {
    if (this.aplicando()) {
      evento.preventDefault();
    }
  }

  protected aplicar(dialogo: HTMLDialogElement): void {
    const operacao = this.operacao();
    if (!operacao || this.aplicando() || !this.operacaoAutorizada()) {
      return;
    }

    this.aplicando.set(true);
    this.erroOperacao.set('');
    this.aviso.set('');
    const alteraPropriaConta = this.dados()?.usuarios.some(usuario =>
      usuario.proprioUsuario && operacao.usuarioIds.includes(usuario.id),
    );
    this.api.atualizar(operacao)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.aplicando.set(false)))
      .subscribe({
        next: () => {
          this.aviso.set('Permissões atualizadas.');
          dialogo.close();
          this.alteracoes.set(new Map());
          if (alteraPropriaConta) {
            this.acesso.atualizar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
              if (this.podeGerenciar()) {
                this.carregar();
              }
            });
          } else {
            this.carregar();
          }
        },
        error: (erro: unknown) => this.erroOperacao.set(
          mensagemErro(erro, 'Não foi possível aplicar as alterações. Nenhuma alteração foi confirmada.'),
        ),
      });
  }

  private atualizarSelecao(ids: Set<number>, id: number, marcado: boolean): Set<number> {
    const selecao = new Set(ids);
    if (marcado) {
      selecao.add(id);
    } else {
      selecao.delete(id);
    }
    return selecao;
  }
}
