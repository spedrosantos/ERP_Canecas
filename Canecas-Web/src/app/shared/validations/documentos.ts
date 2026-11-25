export type TipoPessoa = 'F' | 'J';

export function normalizarDocumento(valor: string): string {
  return valor.replace(/[.\/-]/g, '').trim().toUpperCase();
}

export function documentoValido(valor: string, tipo: TipoPessoa): boolean {
  const documento = normalizarDocumento(valor);
  const padrao = tipo === 'F' ? /^[0-9]{11}$/ : /^[0-9A-Z]{12}[0-9]{2}$/;
  if (!padrao.test(documento) || /^(.)\1+$/.test(documento)) {
    return false;
  }

  const semDigitos = documento.length - 2;
  function calcular(tamanho: number): number {
    let soma = 0;
    for (let i = 0; i < tamanho; i++) {
      const distancia = tamanho - 1 - i;
      const peso = 2 + (tipo === 'F' ? distancia : distancia % 8);
      soma += (documento.charCodeAt(i) - 48) * peso;
    }
    const resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
  }

  return calcular(semDigitos) === Number(documento[semDigitos]) &&
    calcular(semDigitos + 1) === Number(documento[semDigitos + 1]);
}

export function formatarDocumento(valor: string, tipo: TipoPessoa): string {
  if (tipo === 'F') {
    return valor.replace(/[^0-9]/g, '').slice(0, 11)
      .replace(/^(.{3})(?=.)/, '$1.')
      .replace(/^(...\....)(?=.)/, '$1.')
      .replace(/^(...\....\....)(?=.)/, '$1-');
  }

  return valor.replace(/[^0-9A-Za-z]/g, '').toUpperCase().slice(0, 14)
    .replace(/^(.{2})(?=.)/, '$1.')
    .replace(/^(..\....)(?=.)/, '$1.')
    .replace(/^(..\....\....)(?=.)/, '$1/')
    .replace(/^(..\....\....\/.{4})(?=.)/, '$1-');
}
