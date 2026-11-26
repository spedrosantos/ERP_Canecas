import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { featureGuard } from './core/guards/feature.guard';
import { FEATURES } from './core/services/feature.service';
import { Login } from './features/auth/login/login';
import { Home } from './features/home/home';
import { MainLayout } from './layouts/main-layout/main-layout';

export const routes: Routes = [
  {
    path: 'login',
    component: Login
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: '',
    component: MainLayout,
    canActivateChild: [authGuard],
    children: [
      {
        path: 'home',
        component: Home
      },
      {
        path: 'clientes',
        canActivate: [featureGuard],
        data: { feature: FEATURES.clientes },
        loadComponent: () => import('./features/clientes/clientes').then((modulo) => modulo.Clientes)
      },
      {
        path: 'fornecedores',
        canActivate: [featureGuard],
        data: { feature: FEATURES.fornecedores },
        loadComponent: () =>
          import('./features/fornecedores/fornecedores')
            .then(modulo => modulo.Fornecedores)
      },
      {
        path: 'produtos',
        canActivate: [featureGuard],
        data: { feature: FEATURES.produtos },
        loadComponent: () =>
          import('./features/produtos/produtos')
            .then(modulo => modulo.Produtos)
      },
      {
        path: 'vendas',
        canActivate: [featureGuard],
        data: { feature: FEATURES.vendas },
        loadComponent: () => import('./features/vendas/vendas').then(modulo => modulo.Vendas)
      },
      {
        path: 'estoque',
        canActivate: [featureGuard],
        data: { feature: FEATURES.estoque },
        loadComponent: () =>
          import('./features/estoque/estoque')
            .then(modulo => modulo.Estoque)
      },
      {
        path: 'compras',
        canActivate: [featureGuard],
        data: { feature: FEATURES.compras },
        loadComponent: () => import('./features/compras/compras').then(modulo => modulo.Compras)
      },
      {
        path: 'financeiro/contas-a-pagar',
        canActivate: [featureGuard],
        data: { feature: FEATURES.financeiro, tipo: 'PAGAR' },
        loadComponent: () => import('./features/financeiro/financeiro').then(modulo => modulo.Financeiro)
      },
      {
        path: 'financeiro/contas-a-receber',
        canActivate: [featureGuard],
        data: { feature: FEATURES.financeiro, tipo: 'RECEBER' },
        loadComponent: () => import('./features/financeiro/financeiro').then(modulo => modulo.Financeiro)
      },
      {
        path: 'usuarios',
        canActivate: [featureGuard],
        data: { feature: FEATURES.usuarios },
        loadComponent: () => import('./features/usuarios/usuarios').then((modulo) => modulo.Usuarios)
      },
      {
        path: 'permissoes',
        canActivate: [featureGuard],
        data: { feature: FEATURES.permissoes },
        loadComponent: () => import('./features/permissoes/permissoes').then((modulo) => modulo.Permissoes)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
