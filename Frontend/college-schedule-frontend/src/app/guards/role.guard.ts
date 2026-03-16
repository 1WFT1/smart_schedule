import { Injectable } from '@angular/core';
import { Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class RoleGuard  {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const allowedRoles = route.data['roles'] as Array<string>;
    
    if (this.authService.hasRole(allowedRoles)) {
      return true;
    }
    
    // Перенаправляем на соответствующий маршрут в зависимости от роли
    const user = this.authService.getCurrentUser();
    if (user?.role === 'student') {
      this.router.navigate(['/student']);
    } else if (user?.role === 'teacher' || user?.role === 'admin') {
      this.router.navigate(['/curator']);
    }
    
    return false;
  }
}