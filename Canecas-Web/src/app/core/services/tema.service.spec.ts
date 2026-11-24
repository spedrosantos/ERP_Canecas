import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { TemaService } from './tema.service';

describe('TemaService', () => {
  it('usa a logo local da empresa 1 quando a API não informa uma URL', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const service = TestBed.inject(TemaService);
    const http = TestBed.inject(HttpTestingController);
    const tema = { ...service.temaAtual(), nomeFantasia: 'Boracup' };

    service.carregarTema('1');
    expect(service.temaAtual().logoUrl).toBeNull();
    http.expectOne('/api/empresas/1/tema').flush(tema);
    expect(service.temaAtual().logoUrl).toBe('logos/boracup.jpeg');

    service.carregarTema(1);
    http.expectOne('/api/empresas/1/tema').flush({ ...tema, logoUrl: '/logo-api.png' });
    expect(service.temaAtual().logoUrl).toBe('/logo-api.png');

    service.carregarTema(2);
    http.expectOne('/api/empresas/2/tema').flush(tema);
    expect(service.temaAtual().logoUrl).toBeNull();
    http.verify();
  });

  it('restaura o tema ao trocar de empresa e cancela a consulta anterior', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const service = TestBed.inject(TemaService);
    const http = TestBed.inject(HttpTestingController);

    service.carregarTema(1);
    http.expectOne('/api/empresas/1/tema').flush({
      nomeFantasia: 'Empresa A',
      corPrimaria: '#123456',
      corSecundaria: '#654321',
      corFundo: '#ffffff',
      fonteTitulo: 'Fredoka',
      fonteTexto: 'Nunito',
      logoUrl: '/logo.png',
    });
    expect(service.temaAtual().nomeFantasia).toBe('Empresa A');
    expect(document.documentElement.style.getPropertyValue('--fonte-titulo')).toContain('Fredoka');
    expect(document.documentElement.style.getPropertyValue('--fonte-texto')).toContain('Nunito');

    service.carregarTema(2);
    const consultaAnterior = http.expectOne('/api/empresas/2/tema');
    expect(service.temaAtual().nomeFantasia).toBe('Canecas ERP');
    expect(document.documentElement.style.getPropertyValue('--cor-primaria')).toBe('#1F2937');

    service.carregarTema(3);
    expect(consultaAnterior.cancelled).toBe(true);
    http.expectOne('/api/empresas/3/tema').flush(null, { status: 404, statusText: 'Not Found' });
    expect(service.temaAtual().logoUrl).toBeNull();

    service.carregarTema(4);
    const consultaPendente = http.expectOne('/api/empresas/4/tema');
    service.restaurarTemaPadrao();
    expect(consultaPendente.cancelled).toBe(true);
    expect(service.temaAtual().fonteTexto).toBe('Inter');
    http.verify();
  });
});
