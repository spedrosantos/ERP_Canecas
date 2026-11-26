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
  private readonly campo = inject<ElementRef<HTMLInputElement>>(ElementRef).nativeElement;
  private readonly formato = new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
    maximumFractionDigits: 4,
  });
  readonly casasDecimaisMoeda = input<2 | 4>(2);

  private formatarEdicao(valor: number): string {
    return valor.toLocaleString('pt-BR', {
      style: 'currency',
      currency: 'BRL',
      minimumFractionDigits: this.casasDecimaisMoeda(),
      maximumFractionDigits: this.casasDecimaisMoeda(),
    });
  }
  private valor: number | null = null;
  private alterar: (valor: number | null) => void = () => {};
  private tocar: () => void = () => {};

  writeValue(valor: number | null): void {
    this.valor = valor;
    this.campo.value = valor === null ? '' : this.formato.format(valor);
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
    if (this.valor !== null) this.campo.value = this.formatarEdicao(this.valor);
    this.campo.select();
  }

  protected digitar(): void {
    const digitos = this.campo.value.replace(/\D/g, '');
    const unidades = Number(digitos);
    this.valor = digitos && Number.isSafeInteger(unidades) ? unidades / 10 ** this.casasDecimaisMoeda() : null;
    this.campo.value = this.valor === null ? '' : this.formatarEdicao(this.valor);
    const fim = this.campo.value.length;
    this.campo.setSelectionRange(fim, fim);
    this.alterar(this.valor);
  }

  protected finalizar(): void {
    this.writeValue(this.valor);
    this.tocar();
  }
}
