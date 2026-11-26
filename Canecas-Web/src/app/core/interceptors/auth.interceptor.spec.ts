import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('Autenticação em Compras', () => {
  it('envia o JWT a todas as rotas de Compras do ERP e preserva URLs externas', () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { obterToken: () => 'token-de-teste' } }
      ]
    });

    const client = TestBed.inject(HttpClient);
    const http = TestBed.inject(HttpTestingController);
    const api = '/api/compras/pedidos';
    const rotas = [
      ['GET', '?pagina=1'],
      ['POST', ''],
      ['GET', '/1'],
      ['PUT', '/1'],
      ['GET', '/fornecedores?busca=teste'],
      ['GET', '/produtos?busca=teste'],
      ['POST', '/1/cancelar'],
      ['POST', '/1/receber']
    ];

    for (const [method, sufixo] of rotas) {
      const url = api + sufixo;
      client.request(method, url).subscribe();
      const req = http.expectOne({ method, url });
      expect(req.request.headers.get('Authorization')).toBe('Bearer token-de-teste');
      req.flush(null);
    }

    const externa = 'https://example.com' + api;
    client.get(externa).subscribe();
    const req = http.expectOne(externa);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush(null);
    http.verify();
  });
});
