// src/app/services/group.service.ts

import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { Group, CreateGroupDto, UpdateGroupDto } from '../models/group.model';

@Injectable({
  providedIn: 'root'
})
export class GroupService {
  private apiUrl = 'http://localhost:5261/api/groups';
  
  // Только для реактивности, без кэширования
  private groupsSubject = new BehaviorSubject<Group[]>([]);
  public groups$ = this.groupsSubject.asObservable();

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  private getHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  private handleError(error: HttpErrorResponse) {
    console.error('GroupService error:', error);
    
    let errorMessage = 'Произошла ошибка';
    
    if (error.status === 401) {
      errorMessage = 'Необходима авторизация';
    } else if (error.status === 403) {
      errorMessage = 'Нет прав для этого действия';
    } else if (error.status === 404) {
      errorMessage = 'Группа не найдена';
    } else if (error.error?.message) {
      errorMessage = error.error.message;
    }
    
    return throwError(() => ({ 
      message: errorMessage,
      status: error.status
    }));
  }

  // Получить все группы
    getGroups(): Observable<Group[]> {
    return this.http.get<Group[]>(this.apiUrl, { 
        headers: this.getHeaders() 
    }).pipe(
        tap(groups => {
        console.log('Сырые группы из API:', groups);
        this.groupsSubject.next(groups);
        }),
        catchError(this.handleError)
    );
    }

  // Получить группу по ID
  getGroup(id: number): Observable<Group> {
    return this.http.get<Group>(`${this.apiUrl}/${id}`, { 
      headers: this.getHeaders() 
    }).pipe(
      catchError(this.handleError)
    );
  }

  // Создать новую группу
  createGroup(groupData: CreateGroupDto): Observable<Group> {
    return this.http.post<Group>(this.apiUrl, groupData, { 
      headers: this.getHeaders() 
    }).pipe(
      tap(newGroup => {
        const current = this.groupsSubject.value;
        this.groupsSubject.next([...current, newGroup]);
      }),
      catchError(this.handleError)
    );
  }

  // Обновить группу
  updateGroup(id: number, updates: UpdateGroupDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, updates, { 
      headers: this.getHeaders() 
    }).pipe(
      tap(() => {
        const current = this.groupsSubject.value;
        const updated = current.map(g => 
          g.id === id ? { ...g, ...updates } : g
        );
        this.groupsSubject.next(updated);
      }),
      catchError(this.handleError)
    );
  }

  // Удалить группу
  deleteGroup(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`, { 
      headers: this.getHeaders() 
    }).pipe(
      tap(() => {
        const current = this.groupsSubject.value;
        this.groupsSubject.next(current.filter(g => g.id !== id));
      }),
      catchError(this.handleError)
    );
  }

  // Получить студентов группы
  getGroupStudents(groupId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/students/${groupId}`, { 
      headers: this.getHeaders() 
    }).pipe(
      catchError(this.handleError)
    );
  }
}