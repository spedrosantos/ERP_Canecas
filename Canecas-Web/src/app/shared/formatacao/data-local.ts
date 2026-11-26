export function dataAtualParaFormulario(): string {
  const partes = new Intl.DateTimeFormat('pt-BR', {
    timeZone: 'America/Sao_Paulo', year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(new Date());
  const data = Object.fromEntries(partes.map(parte => [parte.type, parte.value]));
  return data['year'] + '-' + data['month'] + '-' + data['day'];
}
