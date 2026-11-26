import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap } from 'rxjs';
import { NotificacoesService } from '../services/notificacoes.service';

function mensagemDaApi(resposta: unknown): string | undefined {
  if (!resposta || typeof resposta !== 'object') return undefined;

  if ('mensagem' in resposta && typeof resposta.mensagem === 'string' && resposta.mensagem.trim()) {
    return resposta.mensagem.trim();
  }

  if ('errors' in resposta && resposta.errors && typeof resposta.errors === 'object') {
    const mensagens = Object.values(resposta.errors).flat().filter(
      (mensagem): mensagem is string => typeof mensagem === 'string' && !!mensagem.trim(),
    );
    return [...new Set(mensagens)].join(' ') || undefined;
  }
  return undefined;
}

function mensagemErro(erro: unknown, login: boolean): string {
  if (!(erro instanceof HttpErrorResponse)) return 'Não foi possível concluir a solicitação.';
  if (erro.status === 0) return 'Sem conexão com o servidor. Tente novamente.';
  if (erro.status === 401) {
    return login ? 'Credenciais inválidas. Confira os dados informados.' : 'Sua sessão expirou. Entre novamente.';
  }
  if (erro.status === 403) return 'Você não tem permissão para esta operação.';
  if (erro.status === 429) {
    return typeof erro.error?.title === 'string' && erro.error.title.trim()
      ? erro.error.title
      : 'Limite de solicitações atingido. Aguarde antes de tentar novamente.';
  }
  if (erro.status >= 500) return 'O servidor não conseguiu concluir a solicitação. Tente novamente.';

  return mensagemDaApi(erro.error) ?? (erro.status === 404
    ? 'O registro solicitado não foi encontrado.'
    : 'Não foi possível concluir a solicitação. Confira os dados informados.');
}

export const notificacoesInterceptor: HttpInterceptorFn = (requisicao, next) => {
  if (!requisicao.url.startsWith('/api/')) return next(requisicao);

  const notificacoes = inject(NotificacoesService);
  const caminho = requisicao.url.split('?', 1)[0].replace(/\/$/, '');
  const login = caminho === '/api/auth/login';
  const consultaUsuario = caminho.startsWith('/api/consultas/');
  const escrita = ['POST', 'PUT', 'PATCH', 'DELETE'].includes(requisicao.method);

  return next(requisicao).pipe(tap({
    next: evento => {
      if (!(evento instanceof HttpResponse) || !evento.ok || (!escrita && !consultaUsuario)) return;

      const mensagem = login ? 'Login realizado com sucesso.'
        : consultaUsuario ? 'Consulta concluída.'
        : 'Operação concluída com sucesso.';
      notificacoes.sucesso(mensagemDaApi(evento.body) ?? mensagem);
    },
    error: (erro: unknown) => notificacoes.erro(mensagemErro(erro, login)),
  }));
};
