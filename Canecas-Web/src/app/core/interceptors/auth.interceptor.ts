import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap } from 'rxjs';

import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  // Somente rotas protegidas conhecidas, usando URLs relativas da aplicação.
  const caminho = request.url.split('?', 1)[0];
  const protegido =
    ((request.method === 'GET' || request.method === 'POST') && /^\/api\/usuarios\/?$/i.test(caminho)) ||
    ((request.method === 'GET' || request.method === 'PUT') &&
      /^\/api\/usuarios\/permissoes\/lote\/?$/i.test(caminho)) ||
    (request.method === 'GET' && /^\/api\/usuarios\/me\/features\/?$/i.test(caminho)) ||
    (request.method === 'PUT' && /^\/api\/usuarios\/\d+\/?$/i.test(caminho)) ||
    (request.method === 'PATCH' && /^\/api\/usuarios\/\d+\/(?:situacao|senha)\/?$/i.test(caminho)) ||
    ((request.method === 'GET' || request.method === 'POST') && /^\/api\/(?:clientes|fornecedores|produtos|categorias|unidades-medida)\/?$/i.test(caminho)) ||
    ((request.method === 'GET' || request.method === 'PUT') && /^\/api\/(?:clientes|fornecedores|produtos|categorias|unidades-medida)\/\d+\/?$/i.test(caminho)) ||
    (request.method === 'PATCH' && /^\/api\/(?:clientes|fornecedores|produtos|categorias|unidades-medida)\/\d+\/situacao\/?$/i.test(caminho)) ||
    (request.method === 'GET' && /^\/api\/consultas\/(?:cep|cnpj)\/?$/i.test(caminho)) ||
    (request.method === 'GET' && /^\/api\/estoque\/(?:saldos|movimentacoes)\/?$/i.test(caminho)) ||
    (request.method === 'POST' && /^\/api\/estoque\/movimentacoes\/ajuste\/?$/i.test(caminho)) ||
    (request.method === 'POST' && /^\/api\/estoque\/movimentacoes\/\d+\/estorno\/?$/i.test(caminho)) ||
    (request.method === 'PATCH' && /^\/api\/estoque\/saldos\/\d+\/minimo\/?$/i.test(caminho));

  if (!protegido) {
    return next(request);
  }

  const auth = inject(AuthService);
  const token = auth.obterToken();
  if (!token) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })).pipe(
    tap({
      error: (erro: unknown) => {
        if (erro instanceof HttpErrorResponse && erro.status === 401) {
          auth.invalidarToken(token);
        }
      },
    }),
  );
};
