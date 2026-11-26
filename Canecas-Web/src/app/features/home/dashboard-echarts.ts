import { BarChart, LineChart, PieChart } from 'echarts/charts';
import { DataZoomComponent, GridComponent, LegendComponent, TooltipComponent } from 'echarts/components';
import { init, use } from 'echarts/core';
import { SVGRenderer } from 'echarts/renderers';

use([BarChart, LineChart, PieChart, DataZoomComponent, GridComponent, LegendComponent, TooltipComponent, SVGRenderer]);

export const iniciarGrafico = (elemento: HTMLElement) => init(elemento, undefined, { renderer: 'svg' });
