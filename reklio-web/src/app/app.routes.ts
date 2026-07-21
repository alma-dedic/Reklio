import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register').then((m) => m.Register),
  },
  {
    path: 'registracija-uspjesna',
    loadComponent: () =>
      import('./features/auth/register-success/register-success').then(
        (m) => m.RegisterSuccess,
      ),
  },
  {
    path: 'kupac',
    canActivate: [authGuard, roleGuard('Customer')],
    loadComponent: () =>
      import('./features/customer/customer-home/customer-home').then(
        (m) => m.CustomerHome,
      ),
  },
  {
    path: 'operater',
    canActivate: [authGuard, roleGuard('Operator')],
    loadComponent: () =>
      import('./features/operator/operator-panel/operator-panel').then(
        (m) => m.OperatorPanel,
      ),
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' },
];