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
      import('./features/customer/customer-layout/customer-layout').then(
        (m) => m.CustomerLayout,
      ),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/customer/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'nova',
        loadComponent: () =>
          import('./features/customer/claim-wizard/claim-wizard').then(
            (m) => m.ClaimWizard,
          ),
      },
      {
        path: 'reklamacija/:id/potvrda',
        loadComponent: () =>
          import(
            './features/customer/claim-confirmation/claim-confirmation'
          ).then((m) => m.ClaimConfirmation),
      },
      {
        path: 'reklamacija/:id',
        loadComponent: () =>
          import('./features/customer/claim-details/claim-details').then(
            (m) => m.ClaimDetails,
          ),
      },
    ],
  },
  {
    path: 'operater',
    canActivate: [authGuard, roleGuard('Operator')],
    loadComponent: () =>
      import('./features/operator/operator-layout/operator-layout').then(
        (m) => m.OperatorLayout,
      ),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/operator/operator-panel/operator-panel').then(
            (m) => m.OperatorPanel,
          ),
      },
    ],
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' },
];
