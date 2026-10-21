import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/login/login').then((component) => component.LoginPage)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/shell/shell').then((component) => component.Shell),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'upload'
      },
      {
        path: 'upload',
        loadComponent: () =>
          import('./features/commerce/upload/upload').then((component) => component.UploadPage)
      },
      {
        path: 'process',
        loadComponent: () =>
          import('./features/commerce/process/process').then((component) => component.ProcessPage)
      },
      {
        path: 'quarantine',
        loadComponent: () =>
          import('./features/commerce/quarantine/quarantine').then(
            (component) => component.QuarantinePage
          )
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
