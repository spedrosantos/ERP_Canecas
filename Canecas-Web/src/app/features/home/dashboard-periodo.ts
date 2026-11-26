import { dataAtualParaFormulario } from '../../shared/formatacao/data-local';
import type { PeriodoDashboard } from './dashboard.service';

export type AtalhoPeriodo = 'hoje' | 'semana' | 'mes' | 'mesAnterior' | 'personalizado';

const DIA = 86_400_000;
const iso = (data: Date) => data.toISOString().slice(0, 10);
const instante = (data: string) => Date.parse(data + 'T00:00:00Z');

export function periodoPredefinido(atalho: Exclude<AtalhoPeriodo, 'personalizado'>, hoje = dataAtualParaFormulario()): PeriodoDashboard {
  if (atalho === 'hoje') return { dataInicio: hoje, dataFim: hoje };
  if (atalho === 'semana') return { dataInicio: iso(new Date(instante(hoje) - 6 * DIA)), dataFim: hoje };
  const [ano, mes] = hoje.split('-').map(Number);
  const indice = mes - (atalho === 'mesAnterior' ? 2 : 1);
  const data = (mes: number, dia: number) => {
    const resultado = new Date(0);
    resultado.setUTCFullYear(ano, mes, dia);
    return iso(resultado);
  };
  return { dataInicio: data(indice, 1), dataFim: data(indice + 1, 0) };
}

export function periodoAnterior(periodo: PeriodoDashboard, atalho: AtalhoPeriodo): PeriodoDashboard | null {
  const inicio = instante(periodo.dataInicio);
  const duracao = instante(periodo.dataFim) - inicio + DIA;
  const anterior = new Date(inicio - duracao);
  if (anterior.getUTCFullYear() < 1) return null;
  if (atalho === 'mes' || atalho === 'mesAnterior') return periodoPredefinido('mesAnterior', periodo.dataInicio);
  return { dataInicio: iso(anterior), dataFim: iso(new Date(inicio - DIA)) };
}

export function granularidadeDiaria(periodo: PeriodoDashboard): boolean {
  return instante(periodo.dataFim) - instante(periodo.dataInicio) < 45 * DIA;
}
