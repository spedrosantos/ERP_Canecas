import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Notificacoes } from '../../shared/components/notificacoes/notificacoes';
import { notificacoesInterceptor } from './notificacoes.interceptor';

describe('Notificações da API', () => {
  it('exibe sucesso de gravação e os detalhes da validação, mantendo consultas comuns silenciosas', () => {
    TestBed.configureTestingModule({
      imports: [Notificacoes],
      providers: [
        provideHttpClient(withInterceptors([notificacoesInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    const fixture = TestBed.createComponent(Notificacoes);
    const http = TestBed.inject(HttpTestingController);
    const client = TestBed.inject(HttpClient);
    const tela: HTMLElement = fixture.nativeElement;

    client.get('/api/produtos').subscribe();
    http.expectOne('/api/produtos').flush([]);
    fixture.detectChanges();
    expect(tela.querySelector('[role="status"]')).toBeNull();

    client.post('/api/produtos', {}).subscribe();
    http.expectOne('/api/produtos').flush(null, { status: 204, statusText: 'No Content' });
    fixture.detectChanges();
    expect(tela.querySelector('[role="status"]')?.textContent).toContain('Operação concluída');
    tela.querySelector('button')!.click();
    fixture.detectChanges();
    expect(tela.querySelector('[role="status"]')).toBeNull();

    let status = 0;
    client.put('/api/produtos/1', {}).subscribe({ error: erro => status = erro.status });
    http.expectOne('/api/produtos/1').flush(
      { errors: { nome: ['Informe o nome do produto.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();
    expect(status).toBe(400);
    expect(tela.querySelector('[role="alert"]')?.textContent).toContain('Informe o nome do produto.');
    http.verify();
  });
});
