import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { Router } from '@angular/router';

export interface User {
  id: number;
  username: string;
  fullName: string;
  role: 'student' | 'teacher' | 'admin';
  group?: string;
  groups?: string[];
  token?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();
  
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  
  private apiUrl = 'http://localhost:5261/api';

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    this.loadStoredUser();
  }

  private loadStoredUser(): void {
    const token = localStorage.getItem('token');
    const currentUser = localStorage.getItem('currentUser');
    
    if (token && currentUser) {
      try {
        const user = JSON.parse(currentUser);
        user.token = token;
        this.currentUserSubject.next(user);
        this.isAuthenticatedSubject.next(true);
      } catch (e) {
        this.logout();
      }
    }
  }

  // Вход студента через журнал
  studentLogin(username: string, password: string): Observable<any> {
    console.log('Попытка входа студента:', username);
    
    return this.http.post(`${this.apiUrl}/Auth/student-login`, { username, password }).pipe(
      tap((response: any) => {
        console.log('Успешный вход студента');
        this.setSession(response);
      }),
      catchError(this.handleError.bind(this))
    );
  }

  // Вход администратора (созданного вручную)
  adminLogin(username: string, password: string): Observable<any> {
    console.log('Попытка входа администратора:', username);
    
    return this.http.post(`${this.apiUrl}/Auth/admin-login`, { username, password }).pipe(
      tap((response: any) => {
        console.log('Успешный вход администратора');
        this.setSession(response);
      }),
      catchError(this.handleError.bind(this))
    );
  }

  private setSession(response: any): void {
    localStorage.setItem('token', response.token);
    localStorage.setItem('currentUser', JSON.stringify(response.user));
    
    this.currentUserSubject.next(response.user);
    this.isAuthenticatedSubject.next(true);
    
    // Перенаправляем в зависимости от роли
    if (response.user.role === 'student') {
      this.router.navigate(['/student']);
    } else {
      this.router.navigate(['/curator']);
    }
  }

  private handleError(error: HttpErrorResponse) {
    let errorMessage = 'Произошла ошибка при входе';
    
    if (error.status === 0) {
      errorMessage = 'Сервер недоступен. Проверьте подключение.';
      console.error('Network error:', error.error);
    } else if (error.status === 401) {
      errorMessage = error.error?.message || 'Неверный логин или пароль';
    } else if (error.status === 404) {
      errorMessage = 'Эндпоинт не найден. Проверьте URL.';
    } else if (error.status === 500) {
      errorMessage = error.error?.message || 'Внутренняя ошибка сервера. Попробуйте позже.';
    } else if (error.error?.message) {
      errorMessage = error.error.message;
    }
    
    console.error('Ошибка входа:', error);
    return throwError(() => ({ 
      message: errorMessage,
      status: error.status,
      error: error.error
    }));
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('currentUser');
    localStorage.removeItem('scheduleEvents');
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    this.router.navigate(['/login']);
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  hasRole(roles: string[]): boolean {
    const user = this.getCurrentUser();
    if (!user) return false;
    return roles.includes(user.role);
  }

  getFirstName(): string {
    const user = this.getCurrentUser();
    if (!user) return 'Пользователь';
    
    const nameParts = user.fullName.split(' ');
    if (nameParts.length >= 2) {
      return nameParts[1];
    }
    return nameParts[0] || 'Пользователь';
  }

  getFullName(): string {
    return this.getCurrentUser()?.fullName || 'Пользователь';
  }

  getUserGroups(): string[] {
    const user = this.getCurrentUser();
    if (!user) return [];
    
    if (user.role === 'student') {
      return user.group ? [user.group] : [];
    }
    return user.groups || [];
  }
}