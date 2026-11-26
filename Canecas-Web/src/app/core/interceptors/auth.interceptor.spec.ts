import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('Autenticação em Compras e Vendas', () => {
  it.each([
    ['compras', 'fornecedores', 'receber'],
    ['vendas', 'clientes', 'confirmar'],
  ])('envia o JWT às rotas de %s do ERP e preserva URLs externas', (modulo, cadastro, transicao) => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { obterToken: () => 'token-de-teste' } }
      ]
    });

    const client = TestBed.inject(HttpClient);
    const http = TestBed.inject(HttpTestingController);
    const api = '/api/' + modulo + '/pedidos';
    const rotas = [
      ['GET', '?pagina=1'],
      ['POST', ''],
      ['GET', '/1'],
      ['PUT', '/1'],
      ['GET', '/' + cadastro + '?busca=teste'],
      ['GET', '/produtos?busca=teste'],
      ['POST', '/1/cancelar'],
      ['POST', '/1/' + transicao]
    ];

    if (modulo === 'vendas') rotas.push(['GET', '/produtos/1/disponibilidade']);

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
