import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap } from 'rxjs';

import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  // Somente rotas protegidas conhecidas, usando URLs relativas da aplicação.
  const caminho = request.url.split('?', 1)[0];
  if (request.method !== 'POST' || !/^\/api\/usuarios\/?$/i.test(caminho)) {
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
