import { Directive, ElementRef, forwardRef, inject, input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Directive({
  selector: 'input[appMoeda]',
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => Moeda),
    multi: true,
  }],
  host: {
    '(focus)': 'editar()',
    '(input)': 'digitar()',
    '(blur)': 'finalizar()',
  },
})
export class Moeda implements ControlValueAccessor {
  readonly preservarPrecisaoMoeda = input(false);
  private readonly campo = inject<ElementRef<HTMLInputElement>>(ElementRef).nativeElement;
  private readonly formato = new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
  private readonly formatoPrecisao = new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
    maximumFractionDigits: 4,
  });
  private valor: number | null = null;
  private alterar: (valor: number | null) => void = () => {};
  private tocar: () => void = () => {};

  writeValue(valor: number | null): void {
    this.valor = valor;
    const formato = this.preservarPrecisaoMoeda() ? this.formatoPrecisao : this.formato;
    this.campo.value = valor === null ? '' : formato.format(valor);
  }

  registerOnChange(alterar: (valor: number | null) => void): void {
    this.alterar = alterar;
  }

  registerOnTouched(tocar: () => void): void {
    this.tocar = tocar;
  }

  setDisabledState(desabilitado: boolean): void {
    this.campo.disabled = desabilitado;
  }

  protected editar(): void {
    if (this.preservarPrecisaoMoeda() && this.valor !== null) {
      this.campo.value = this.formato.format(this.valor);
    }
    this.campo.select();
  }

  protected digitar(): void {
    const digitos = this.campo.value.replace(/\D/g, '');
    const centavos = Number(digitos);
    this.writeValue(digitos && Number.isSafeInteger(centavos) ? centavos / 100 : null);
    const fim = this.campo.value.length;
    this.campo.setSelectionRange(fim, fim);
    this.alterar(this.valor);
  }

  protected finalizar(): void {
    this.writeValue(this.valor);
    this.tocar();
  }
}
