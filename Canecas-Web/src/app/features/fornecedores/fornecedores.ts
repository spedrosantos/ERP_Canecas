import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, computed, DestroyRef, effect, ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Mascara } from '../../shared/directives/mascara';
import { catchError, debounceTime, distinctUntilChanged, finalize, map, Observable, of, Subject, switchMap, takeUntil } from 'rxjs';

import { FEATURES, FeatureService } from '../../core/services/feature.service';
import { Fornecedor, FornecedoresPaginados, DadosFornecedor, EnderecoConsulta, FiltroFornecedores, TipoPessoa } from './fornecedor';
import { FornecedoresService } from './fornecedores.service';
import { documentoValido, formatarDocumento, normalizarDocumento } from '../../shared/validations/documentos';

type CampoConsulta = keyof (EnderecoConsulta & Pick<DadosFornecedor, 'nome' | 'nomeFantasia'>);
type Sugestao = Partial<Record<CampoConsulta, string | null>>;
type Confirmacao =
  | { tipo: 'situacao'; fornecedor: Fornecedor }
  | { tipo: 'consulta'; campos: Partial<Record<CampoConsulta, string>> };
type Consulta = 'cep' | 'cnpj';

function mensagemErro(erro: unknown, padrao: string): string {
  if (!(erro instanceof HttpErrorResponse)) {
    return padrao;
  }
  switch (erro.status) {
    case 0:
      return 'Não foi possível conectar ao serviço.';
    case 403:
      return 'Você não tem permissão para realizar esta operação.';
    case 409:
    case 429:
      return typeof erro.error?.title === 'string' ? erro.error.title : padrao;
    case 503:
      return 'Serviço indisponível. Tente novamente.';
    case 504:
      return 'O serviço não respondeu a tempo.';
    default:
      return padrao;
  }
}

@Component({
  selector: 'app-fornecedores',
  imports: [ReactiveFormsModule, Mascara],
  templateUrl: './fornecedores.html',
  styleUrl: './fornecedores.scss',
})
export class Fornecedores {
  private readonly api = inject(FornecedoresService);
  private readonly features = inject(FeatureService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly detector = inject(ChangeDetectorRef);
  private readonly fb = inject(FormBuilder).nonNullable;
  private readonly editor = viewChild<ElementRef<HTMLDialogElement>>('editor');
  private readonly confirmacao = viewChild<ElementRef<HTMLDialogElement>>('confirmacao');
  private readonly requisicoesLista = new Subject<FiltroFornecedores>();
  private readonly cancelarConsultas = new Subject<void>();
  private readonly cnpjDigitado = new Subject<string>();
  private cnpjAguardandoConfirmacao = '';

  protected readonly podeGerenciar = computed(() => this.features.tem(FEATURES.fornecedores));
  protected readonly carregando = signal(false);
  protected readonly salvando = signal(false);
  protected readonly consultando = signal<Consulta | null>(null);
  protected readonly erroLista = signal('');
  protected readonly erroEditor = signal('');
  protected readonly erroConfirmacao = signal('');
  protected readonly avisoConsulta = signal('');
  protected readonly aviso = signal('');
  protected readonly fornecedorEdicao = signal<Fornecedor | null>(null);
  protected readonly pedido = signal<Confirmacao | null>(null);
  protected readonly resultados = signal<FornecedoresPaginados | null>(null);
  protected readonly ultimaPagina = computed(() => {
    const dados = this.resultados();
    return dados ? Math.max(1, Math.ceil(dados.total / dados.tamanhoPagina)) : 1;
  });
  protected readonly pagina = signal(1);
  protected readonly filtros = this.fb.group({
    busca: '',
    tipoPessoa: this.fb.control<TipoPessoa | ''>(''),
    situacao: this.fb.control<'todos' | 'ativos' | 'inativos'>('ativos'),
    tamanhoPagina: 20,
  });
  protected readonly formulario = this.fb.group({
    tipoPessoa: this.fb.control<TipoPessoa>('F'),
    nome: ['', [Validators.required, Validators.maxLength(150), Validators.pattern(/\S/)]],
    nomeFantasia: ['', Validators.maxLength(150)],
    cpfCnpj: ['', Validators.required],
    telefone: ['', Validators.maxLength(20)],
    email: ['', Validators.maxLength(150)],
    cep: ['', Validators.pattern(/^[0-9]{5}-?[0-9]{3}$/)],
    logradouro: ['', Validators.maxLength(200)],
    numero: ['', Validators.maxLength(20)],
    complemento: ['', Validators.maxLength(100)],
    bairro: ['', Validators.maxLength(100)],
    cidade: ['', Validators.maxLength(100)],
    uf: ['', Validators.maxLength(2)],
  });

  protected readonly camposEndereco = [
    { campo: 'logradouro', rotulo: 'Logradouro', limite: 200 },
    { campo: 'numero', rotulo: 'Número', limite: 20 },
    { campo: 'complemento', rotulo: 'Complemento', limite: 100 },
    { campo: 'bairro', rotulo: 'Bairro', limite: 100 },
    { campo: 'cidade', rotulo: 'Cidade', limite: 100 },
    { campo: 'uf', rotulo: 'UF', limite: 2 },
  ] as const;
  protected readonly formatarDocumento = formatarDocumento;

  constructor() {
    const { cpfCnpj, tipoPessoa } = this.formulario.controls;
    cpfCnpj.addValidators(control => documentoValido(control.value, tipoPessoa.value) ? null : { documento: true });
    this.requisicoesLista.pipe(
      switchMap(filtro => {
        this.carregando.set(true);
        this.erroLista.set('');
        return this.api.listar(filtro).pipe(
          switchMap(dados => {
            const ultima = Math.max(1, Math.ceil(dados.total / dados.tamanhoPagina));
            return filtro.pagina > ultima ? this.api.listar({ ...filtro, pagina: ultima }) : of(dados);
          }),
          catchError(erro => {
            this.erroLista.set(mensagemErro(erro, 'Não foi possível carregar os fornecedores.'));
            return of(null);
          }),
          finalize(() => this.carregando.set(false)),
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(dados => {
      this.resultados.set(dados);
      if (dados) {
        this.pagina.set(dados.pagina);
      }
    });

    this.filtros.valueChanges.pipe(debounceTime(250), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.carregar(1));
    this.cnpjDigitado.pipe(
      distinctUntilChanged(),
      debounceTime(350),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(cnpj => this.consultarCnpjAutomaticamente(cnpj));
    this.carregar(1);

    effect(() => {
      const editor = this.editor()?.nativeElement;
      const confirmacao = this.confirmacao()?.nativeElement;
      if (editor?.open && !this.podeGerenciar()) {
        confirmacao?.close();
        editor.close();
      } else if (confirmacao?.open && !this.podeGerenciar()) {
        confirmacao.close();
      }
    });
  }

  protected carregar(pagina = this.pagina()): void {
    const { busca, tipoPessoa, situacao, tamanhoPagina } = this.filtros.getRawValue();
    this.requisicoesLista.next({
      pagina,
      tamanhoPagina,
      busca: busca.trim() || undefined,
      tipoPessoa: tipoPessoa || undefined,
      ativo: situacao === 'todos' ? undefined : situacao === 'ativos',
    });
  }

  protected abrirEditor(fornecedor: Fornecedor | null = null): void {
    if (!this.podeGerenciar()) {
      return;
    }
    if (fornecedor && !this.codigoValido(fornecedor.id)) {
      return;
    }

    this.reiniciarConsultas();
    this.fornecedorEdicao.set(fornecedor);
    this.erroEditor.set('');
    this.avisoConsulta.set('');
    this.formulario.reset();
    if (fornecedor) {
      this.formulario.setValue({
        tipoPessoa: fornecedor.tipoPessoa, nome: fornecedor.nome, nomeFantasia: fornecedor.nomeFantasia ?? '',
        cpfCnpj: formatarDocumento(fornecedor.cpfCnpj, fornecedor.tipoPessoa),
        telefone: fornecedor.telefone ?? '', email: fornecedor.email ?? '', cep: fornecedor.cep ?? '',
        logradouro: fornecedor.logradouro ?? '', numero: fornecedor.numero ?? '', complemento: fornecedor.complemento ?? '',
        bairro: fornecedor.bairro ?? '', cidade: fornecedor.cidade ?? '', uf: fornecedor.uf ?? '',
      });
    }
    this.detector.detectChanges();
    this.editor()?.nativeElement.showModal();
  }

  protected trocarTipo(tipo: string): void {
    if ((tipo !== 'F' && tipo !== 'J') || tipo === this.formulario.controls.tipoPessoa.value) {
      return;
    }
    this.formulario.controls.tipoPessoa.setValue(tipo);
    this.formulario.controls.cpfCnpj.reset();
    this.avisoConsulta.set('');
    this.cnpjDigitado.next('');
    this.cnpjAguardandoConfirmacao = '';
    if (this.consultando() === 'cnpj') {
      this.cancelarConsultas.next();
    }
  }

  protected digitarDocumento(input: HTMLInputElement): void {
    const tipo = this.formulario.controls.tipoPessoa.value;
    const cursor = input.selectionStart ?? input.value.length;
    const antes = formatarDocumento(input.value.slice(0, cursor), tipo);
    const completo = formatarDocumento(input.value, tipo);
    if (input.value !== completo) {
      input.value = completo;
      input.setSelectionRange(antes.length, antes.length);
    }
    const controle = this.formulario.controls.cpfCnpj;
    const mudou = normalizarDocumento(controle.value) !== normalizarDocumento(completo);
    controle.setValue(completo);
    controle.markAsDirty();
    if (tipo === 'J' && mudou) {
      this.avisoConsulta.set('');
      this.cnpjDigitado.next(normalizarDocumento(completo));
      if (this.consultando() === 'cnpj') {
        this.cancelarConsultas.next();
      }
    }
  }

  protected fecharEditor(): void {
    this.reiniciarConsultas();
    this.formulario.reset();
  }

  protected salvarEditor(): void {
    if (this.salvando() || !this.podeGerenciar()) {
      return;
    }

    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) {
      return;
    }

    const fornecedor = this.fornecedorEdicao();
    const { tipoPessoa, nome, nomeFantasia, cpfCnpj, telefone, email, cep, logradouro, numero, complemento,
      bairro, cidade, uf } = this.formulario.getRawValue();
    const limpar = (valor: string) => valor.trim() || null;
    const dados: DadosFornecedor = {
      tipoPessoa, nome: nome.trim(), nomeFantasia: limpar(nomeFantasia),
      cpfCnpj: normalizarDocumento(cpfCnpj), telefone: limpar(telefone), email: limpar(email),
      cep: limpar(cep.replace('-', '')), logradouro: limpar(logradouro), numero: limpar(numero),
      complemento: limpar(complemento), bairro: limpar(bairro), cidade: limpar(cidade), uf: limpar(uf),
    };
    this.reiniciarConsultas();
    this.salvar(
      fornecedor ? this.api.editar(fornecedor.id, dados) : this.api.cadastrar(dados),
      this.editor()?.nativeElement, this.erroEditor, fornecedor ? 'Fornecedor atualizado.' : 'Fornecedor cadastrado.',
    );
  }

  protected consultar(tipo: Consulta): void {
    if (this.consultando() || this.salvando()) {
      return;
    }
    const controles = this.formulario.controls;
    const controle = tipo === 'cep' ? controles.cep : controles.cpfCnpj;
    const normalizar = tipo === 'cep' ? (cep: string) => cep.trim().replace('-', '') : normalizarDocumento;
    const valor = normalizar(controle.value);
    if (tipo === 'cep' ? !/^[0-9]{8}$/.test(valor)
        : controles.tipoPessoa.value !== 'J' || !documentoValido(valor, 'J')) {
      controle.markAsTouched();
      this.avisoConsulta.set(tipo === 'cep' ? 'Informe um CEP válido.' : 'Informe um CNPJ válido.');
      return;
    }

    this.avisoConsulta.set('');
    this.consultando.set(tipo);
    const consulta = tipo === 'cep'
      ? this.api.consultarCep(valor)
      : this.api.consultarCnpj(valor).pipe(map(dados => ({
        ...dados.endereco, nome: dados.razaoSocial, nomeFantasia: dados.nomeFantasia,
      })));

    consulta.pipe(
      takeUntil(this.cancelarConsultas),
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.consultando.set(null)),
    ).subscribe({
      next: dados => {
        if (normalizar(controle.value) === valor && this.podeGerenciar()) {
          this.aplicarSugestao(dados);
        }
      },
      error: erro => {
        if (normalizar(controle.value) === valor) {
          const documento = tipo.toUpperCase();
          this.avisoConsulta.set(mensagemErro(erro, erro instanceof HttpErrorResponse && erro.status === 404
            ? `${documento} não encontrado.` : `Não foi possível consultar o ${documento}.`));
        }
      },
    });
  }

  protected confirmarSituacao(fornecedor: Fornecedor): void {
    if (this.podeGerenciar() && this.codigoValido(fornecedor.id)) {
      this.abrirConfirmacao({ tipo: 'situacao', fornecedor });
    }
  }

  protected confirmarAcao(): void {
    const pedido = this.pedido();
    const dialogo = this.confirmacao()?.nativeElement;
    if (!pedido || this.salvando() || !this.podeGerenciar()) {
      return;
    }
    if (pedido.tipo === 'consulta') {
      this.formulario.patchValue(pedido.campos);
      dialogo?.close();
    } else {
      const ativo = !pedido.fornecedor.ativo;
      this.salvar(
        this.api.alterarSituacao(pedido.fornecedor.id, ativo), dialogo, this.erroConfirmacao,
        ativo ? 'Fornecedor ativado.' : 'Fornecedor inativado.',
      );
    }
  }

  protected cancelarDialogo(evento: Event): void {
    if (this.salvando()) {
      evento.preventDefault();
    }
  }

  protected retomarConsultaCnpj(): void {
    const cnpj = this.cnpjAguardandoConfirmacao;
    this.cnpjAguardandoConfirmacao = '';
    this.consultarCnpjAutomaticamente(cnpj);
  }

  private reiniciarConsultas(): void {
    this.cnpjDigitado.next('');
    this.cnpjAguardandoConfirmacao = '';
    this.cancelarConsultas.next();
  }

  private consultarCnpjAutomaticamente(cnpj: string): void {
    const { tipoPessoa, cpfCnpj } = this.formulario.controls;
    if (!documentoValido(cnpj, 'J') || tipoPessoa.value !== 'J'
        || cnpj !== normalizarDocumento(cpfCnpj.value)
        || this.salvando() || !this.editor()?.nativeElement.open) {
      return;
    }

    if (this.confirmacao()?.nativeElement.open) {
      this.cnpjAguardandoConfirmacao = cnpj;
      return;
    }

    if (this.consultando() === 'cep') {
      this.cancelarConsultas.next();
    }
    this.consultar('cnpj');
  }

  private aplicarSugestao(dados: Sugestao): void {
    const atuais = this.formulario.getRawValue();
    const vazios: Partial<Record<CampoConsulta, string>> = {};
    const conflitos: Partial<Record<CampoConsulta, string>> = {};
    for (const [campo, dado] of Object.entries(dados) as [CampoConsulta, string | null][]) {
      const atual = atuais[campo].trim();
      const sugestao = dado?.trim();
      if (!sugestao || (campo === 'cep' ? atual.replace('-', '') === sugestao.replace('-', '') : atual === sugestao)) {
        continue;
      }
      (atual ? conflitos : vazios)[campo] = sugestao;
    }
    this.formulario.patchValue(vazios);
    if (Object.keys(conflitos).length) {
      this.abrirConfirmacao({ tipo: 'consulta', campos: conflitos });
    } else {
      this.avisoConsulta.set('Consulta concluída.');
    }
  }

  private abrirConfirmacao(pedido: Confirmacao): void {
    this.pedido.set(pedido);
    this.erroConfirmacao.set('');
    this.detector.detectChanges();
    this.confirmacao()?.nativeElement.showModal();
  }

  private codigoValido(id: number): boolean {
    if (Number.isSafeInteger(id) && id > 0) {
      return true;
    }
    this.aviso.set('O código deste fornecedor não pode ser processado com segurança no navegador.');
    return false;
  }

  private salvar(
    operacao: Observable<Fornecedor>, dialogo: HTMLDialogElement | undefined,
    destinoErro: typeof this.erroEditor, sucesso: string,
  ): void {
    this.salvando.set(true);
    this.aviso.set('');
    destinoErro.set('');
    operacao.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.salvando.set(false)),
    ).subscribe({
      next: () => {
        dialogo?.close();
        this.aviso.set(sucesso);
        this.carregar();
      },
      error: erro => destinoErro.set(mensagemErro(erro, erro instanceof HttpErrorResponse && erro.status === 404
        ? 'Fornecedor não encontrado.' : 'Não foi possível salvar a alteração.')),
    });
  }
}
