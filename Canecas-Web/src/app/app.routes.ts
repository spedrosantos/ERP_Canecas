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
