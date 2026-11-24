import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { FeatureService } from '../services/feature.service';

export const featureGuard: CanActivateFn = (rota) => {
  const auth = inject(AuthService);
  const features = inject(FeatureService);
  const router = inject(Router);
  const codigo: unknown = rota.data['feature'];

  if (!auth.obterToken()) {
    return router.createUrlTree(['/login']);
  }
  if (typeof codigo !== 'string') {
    return router.createUrlTree(['/home']);
  }

  return features.atualizar().pipe(map(() => {
    if (!auth.obterToken()) {
      return router.createUrlTree(['/login']);
    }

    return features.permiteRota(codigo) || router.createUrlTree(['/home']);
  }));
};
