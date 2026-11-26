import type { BarSeriesOption, LineSeriesOption, PieSeriesOption } from 'echarts/charts';
import type {
  DataZoomComponentOption, GridComponentOption, LegendComponentOption, TooltipComponentOption,
} from 'echarts/components';
import type { ComposeOption } from 'echarts/core';
import type { IndicadorFinanceiro } from '../financeiro/financeiro.service';
import type { ResumoCompras, ResumoVendas } from './dashboard.service';

export type OpcoesGrafico = ComposeOption<BarSeriesOption | LineSeriesOption | PieSeriesOption |
  DataZoomComponentOption | GridComponentOption | LegendComponentOption | TooltipComponentOption>;

const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const escala = new Intl.NumberFormat('pt-BR', { notation: 'compact', maximumFractionDigits: 1 });
const quantidade = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 3 });

function base(cores: readonly string[]): OpcoesGrafico {
  return {
    color: [...cores], animation: false,
    textStyle: { fontFamily: 'system-ui, sans-serif', color: '#475569' },
    legend: { bottom: 0, type: 'scroll', textStyle: { color: '#475569' } },
    tooltip: {
      trigger: 'axis', renderMode: 'richText', confine: true,
      valueFormatter: valor => typeof valor === 'number' ? moeda.format(valor) : 'Indisponível',
    },
  };
}

function cartesiano(cores: readonly string[], datas: string[], diaria: boolean): OpcoesGrafico {
  const ampliar = datas.length > 16;
  return {
    ...base(cores),
    grid: { left: 66, right: 18, top: 17, bottom: ampliar ? 75 : 55 },
    xAxis: {
      type: 'category', data: datas, axisTick: { show: false },
      axisLine: { lineStyle: { color: '#dbe2ea' } },
      axisLabel: { formatter: valor => diaria ? valor.slice(8, 10) + '/' + valor.slice(5, 7)
        : valor.slice(5, 7) + '/' + valor.slice(0, 4), hideOverlap: true },
    },
    yAxis: {
      type: 'value', splitLine: { lineStyle: { color: '#e8edf3' } },
      axisLabel: { formatter: (valor: number) => 'R$ ' + escala.format(valor) },
    },
    dataZoom: ampliar ? [
      { type: 'inside', zoomOnMouseWheel: 'ctrl' },
      { type: 'slider', height: 13, bottom: 34, showDetail: false, brushSelect: false },
    ] : undefined,
  };
}

export function graficoFaturamento(vendas: ResumoVendas, diaria: boolean, cores: readonly string[]): OpcoesGrafico | null {
  if (!vendas.evolucao.length) return null;
  return {
    ...cartesiano(cores, vendas.evolucao.map(item => item.mes), diaria),
    series: [
      { name: 'Faturamento', type: 'line', symbolSize: 6, areaStyle: { opacity: 0.16 },
        data: vendas.evolucao.map(item => item.valor) },
      { name: 'Lucro bruto', type: 'line', symbolSize: 6, areaStyle: { opacity: 0.1 },
        data: vendas.evolucao.map(item => item.lucroBruto ?? null) },
    ],
  };
}

export function graficoComprasVendas(
  vendas: ResumoVendas | undefined, compras: ResumoCompras | undefined, diaria: boolean, cores: readonly string[],
): OpcoesGrafico | null {
  const valoresVenda = new Map(vendas?.evolucao.map(item => [item.mes, item.valor]));
  const valoresCompra = new Map(compras?.evolucao.map(item => [item.mes, item.valor]));
  const datas = [...new Set([...valoresVenda.keys(), ...valoresCompra.keys()])].sort();
  if (!datas.length) return null;
  const series: BarSeriesOption[] = [];
  if (vendas) series.push({
    name: 'Vendas', type: 'bar', barMaxWidth: 28, itemStyle: { color: cores[0], borderRadius: [4, 4, 0, 0] },
    data: datas.map(dia => valoresVenda.get(dia) ?? null),
  });
  if (compras) series.push({
    name: 'Compras', type: 'bar', barMaxWidth: 28, itemStyle: { color: cores[2], borderRadius: [4, 4, 0, 0] },
    data: datas.map(dia => valoresCompra.get(dia) ?? null),
  });
  return { ...cartesiano(cores, datas, diaria), series };
}

export function situacoesFinanceiras(titulos: IndicadorFinanceiro) {
  return [
    { name: 'Baixado', value: titulos.valorBaixado },
    { name: 'A vencer', value: Math.max(0, titulos.saldo - titulos.saldoVencido) },
    { name: 'Vencido', value: titulos.saldoVencido },
  ];
}

export function graficoFinanceiro(titulos: IndicadorFinanceiro, cores: readonly string[]): OpcoesGrafico | null {
  const valores = situacoesFinanceiras(titulos);
  if (!valores.some(item => item.value > 0)) return null;
  return {
    ...base([cores[1], cores[0], '#b42340']),
    tooltip: { ...base(cores).tooltip, trigger: 'item' },
    series: [{
      type: 'pie', radius: ['50%', '72%'], center: ['50%', '44%'],
      label: { show: false }, labelLine: { show: false }, stillShowZeroSum: false,
      itemStyle: { borderColor: '#fff', borderWidth: 3, borderRadius: 5 },
      data: valores,
    }],
  };
}

export function graficoRanking(vendas: ResumoVendas, cores: readonly string[]): OpcoesGrafico | null {
  if (!vendas.produtosMaisVendidos.length) return null;
  return {
    ...base(cores),
    legend: { ...base(cores).legend, data: ['Quantidade vendida'] },
    tooltip: {
      ...base(cores).tooltip, trigger: 'item',
      valueFormatter: valor => typeof valor === 'number' ? quantidade.format(valor) : '—',
    },
    grid: { left: 126, right: 55, top: 10, bottom: 34 },
    xAxis: { type: 'value', show: false },
    yAxis: {
      type: 'category', inverse: true, axisTick: { show: false }, axisLine: { show: false },
      data: vendas.produtosMaisVendidos.map(produto => produto.nomeProduto ?? 'Produto #' + produto.produtoId),
      axisLabel: { width: 112, overflow: 'truncate' },
    },
    series: [{
      name: 'Quantidade vendida', type: 'bar', barMaxWidth: 22,
      label: { show: true, position: 'right', formatter: dado => quantidade.format(Number(dado.value)) },
      itemStyle: { borderRadius: [0, 4, 4, 0] },
      data: vendas.produtosMaisVendidos.map(produto => produto.quantidade),
    }],
  };
}
