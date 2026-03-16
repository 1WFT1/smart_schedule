import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { RoleGuard } from './guards/role.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'student',
    loadComponent: () => import('./student/student.component').then(m => m.StudentComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['student'] }
  },
  {
    path: 'curator',
    loadComponent: () => import('./curator/curator-panel.component').then(m => m.CuratorPanelComponent),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['teacher', 'admin'] }
  },
  {
    path: '',
    redirectTo: '/login',
    pathMatch: 'full'
  }
];