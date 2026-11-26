import { Directive, ElementRef, forwardRef, inject, input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

type Valor = string | number | null;

const formatoDecimal = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 3 });

function formatarTelefone(valor: string): string {
  if (valor.startsWith('+')) return '+' + valor.replace(/\D/g, '').slice(0, 15);

  const digitos = valor.replace(/\D/g, '').slice(0, 11);
  if (digitos.length < 3) return digitos;
  const tamanhoPrefixo = digitos.length === 11 ? 7 : 6;
  const final = digitos.slice(tamanhoPrefixo);
  return '(' + digitos.slice(0, 2) + ') ' + digitos.slice(2, tamanhoPrefixo) + (final ? '-' + final : '');
}

function formatarCep(valor: string): string {
  return valor.replace(/\D/g, '').slice(0, 8).replace(/^(\d{5})(?=\d)/, '$1-');
}

@Directive({
  selector: 'input[appMascara]',
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => Mascara),
    multi: true,
  }],
  host: {
    '(input)': 'digitar()',
    '(blur)': 'finalizar()',
  },
})
export class Mascara implements ControlValueAccessor {
  readonly appMascara = input.required<'cep' | 'telefone' | 'decimal'>();
  private readonly campo = inject<ElementRef<HTMLInputElement>>(ElementRef).nativeElement;
  private valor: Valor = null;
  private alterar: (valor: Valor) => void = () => {};
  private tocar: () => void = () => {};

  writeValue(valor: Valor): void {
    this.valor = valor;
    const tipo = this.appMascara();
    if (tipo === 'decimal') {
      this.campo.value = typeof valor === 'number' ? formatoDecimal.format(valor) : '';
    } else if (tipo === 'cep') {
      this.campo.value = formatarCep(String(valor ?? ''));
    } else {
      const telefone = String(valor ?? '');
      // Mantém visíveis os telefones legados fora dos formatos reconhecidos.
      this.campo.value = /^\+?[0-9\s().-]*$/.test(telefone) &&
        telefone.replace(/\D/g, '').length <= (telefone.startsWith('+') ? 15 : 11)
        ? formatarTelefone(telefone)
        : telefone;
    }
  }

  registerOnChange(alterar: (valor: Valor) => void): void {
    this.alterar = alterar;
  }

  registerOnTouched(tocar: () => void): void {
    this.tocar = tocar;
  }

  setDisabledState(desabilitado: boolean): void {
    this.campo.disabled = desabilitado;
  }

  protected digitar(): void {
    const anterior = this.campo.value;
    const cursor = this.campo.selectionStart ?? anterior.length;
    const tipo = this.appMascara();

    if (tipo === 'decimal') {
      const [inteiro, ...fracao] = anterior.replace(/[^\d,]/g, '').split(',');
      const decimais = fracao.join('').slice(0, 3);
      this.campo.value = fracao.length ? inteiro + ',' + decimais : inteiro;
      this.valor = inteiro || decimais ? Number(inteiro + '.' + decimais) : null;
    } else {
      this.campo.value = tipo === 'cep' ? formatarCep(anterior) : formatarTelefone(anterior);
      this.valor = this.campo.value.replace(tipo === 'cep' ? /\D/g : /[^\d+]/g, '');
    }

    const caracteres = anterior.slice(0, cursor).replace(/[^\d,+]/g, '').length;
    const editaveis = Array.from(this.campo.value.matchAll(/[\d,+]/g));
    const posicao = caracteres === 0 ? 0 : (editaveis[caracteres - 1]?.index ?? this.campo.value.length - 1) + 1;
    this.campo.setSelectionRange(posicao, posicao);
    this.alterar(this.valor);
  }

  protected finalizar(): void {
    this.writeValue(this.valor);
    this.tocar();
  }
}
