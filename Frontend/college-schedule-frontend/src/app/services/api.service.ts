import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ScheduleEvent , mapApiEventToEvent } from '../models/event.model';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private apiUrl = (window as any).env?.API_URL || 'http://localhost:5261/api';

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
    let errorMessage = 'Произошла ошибка';
    
    if (error.status === 0) {
      // Ошибка сети
      errorMessage = 'Сервер недоступен. Проверьте подключение.';
      console.error('Network error:', error.error);
    } else if (error.status === 401) {
      // Неавторизован
      errorMessage = 'Сессия истекла. Войдите заново.';
      this.authService.logout();
    } else if (error.status === 403) {
      // Доступ запрещен
      errorMessage = 'У вас нет прав для этого действия.';
    } else if (error.status === 404) {
      // Не найдено
      errorMessage = 'Ресурс не найден.';
    } else if (error.status === 500) {
      // Ошибка сервера
      errorMessage = 'Внутренняя ошибка сервера.';
    } else if (error.error?.message) {
      // Сообщение от сервера
      errorMessage = error.error.message;
    }
    
    console.error('API Error:', error);
    
    // Показываем уведомление пользователю
    this.showErrorNotification(errorMessage);
    
    return throwError(() => ({ 
      message: errorMessage,
      status: error.status 
    }));
  }

  private showErrorNotification(message: string): void {
    // Создаем временное уведомление
    const notification = document.createElement('div');
    notification.style.position = 'fixed';
    notification.style.top = '20px';
    notification.style.right = '20px';
    notification.style.backgroundColor = '#ef4444';
    notification.style.color = 'white';
    notification.style.padding = '12px 20px';
    notification.style.borderRadius = '8px';
    notification.style.boxShadow = '0 4px 12px rgba(239, 68, 68, 0.3)';
    notification.style.zIndex = '9999';
    notification.style.fontWeight = '500';
    notification.style.animation = 'slideIn 0.3s ease';
    notification.textContent = message;
    
    // Добавляем стиль для анимации
    const style = document.createElement('style');
    style.textContent = `
      @keyframes slideIn {
        from { transform: translateX(100%); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
      }
    `;
    document.head.appendChild(style);
    
    document.body.appendChild(notification);
    
    // Удаляем через 5 секунд
    setTimeout(() => {
      notification.style.animation = 'slideOut 0.3s ease forwards';
      setTimeout(() => {
        document.body.removeChild(notification);
      }, 300);
    }, 5000);
  }

  // Аутентификация
  login(username: string, password: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/login`, { username, password }).pipe(
      catchError(this.handleError.bind(this))
    );
  }

  quickLogin(role: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/quick-login`, { role }).pipe(
      catchError(this.handleError.bind(this))
    );
  }

  // Для студентов - расписание на сегодня (пары из журнала)
  getTodaySchedule(): Observable<ScheduleEvent[]> {
    const headers = this.getHeaders();
    console.log('Запрос расписания из журнала');
    
    return this.http.get<any[]>(`${this.apiUrl}/Schedule/today`, { headers }).pipe(
      map((events: any[]) => {
        console.log('Получены пары из журнала:', events);
        return events.map(mapApiEventToEvent);
      }),
      catchError(this.handleError.bind(this))
    );
  }

  // Расписание на конкретную дату (пары из журнала)
  getDaySchedule(date: string): Observable<ScheduleEvent[]> {
    const headers = this.getHeaders();
    return this.http.get<any[]>(`${this.apiUrl}/Schedule/day?date=${date}`, { headers }).pipe(
      map((events: any[]) => events.map(mapApiEventToEvent)),
      catchError(this.handleError.bind(this))
    );
  }

  // Получить мероприятия (внеурочка из вашей БД)
  getEvents(date?: string): Observable<ScheduleEvent[]> {
    const headers = this.getHeaders();
    let url = `${this.apiUrl}/Events`;
    
    // Если дата не передана, используем сегодняшнюю
    const targetDate = date || this.formatDateForApi(new Date());
    url += `?date=${targetDate}`;
    
    console.log('Запрос мероприятий из БД:', url);
    
    return this.http.get<any[]>(url, { headers }).pipe(
      map((events: any[]) => {
        console.log('Получены мероприятия из БД:', events);
        return events.map(mapApiEventToEvent);
      }),
      catchError(this.handleError.bind(this))
    );
  }

  // Добавить метод для получения расписания группы
  getGroupWeekSchedule(groupName: string, startDate: string): Observable<any> {
    const headers = this.getHeaders();
    const url = `${this.apiUrl}/Schedule/group/${encodeURIComponent(groupName)}/week?startDate=${startDate}`;
    console.log('Запрос расписания группы:', url);
    return this.http.get(url, { headers });
  }

  refreshGroupCache(groupName: string): Observable<any> {
    const headers = this.getHeaders();
    return this.http.post(
      `${this.apiUrl}/Schedule/cache/refresh/${encodeURIComponent(groupName)}`, 
      {}, 
      { headers }
    );
  }

  // Вспомогательный метод для форматирования даты
  private formatDateForApi(date: Date): string {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  // Создать событие (для кураторов)
  createEvent(eventData: any): Observable<ScheduleEvent> {
    const headers = this.getHeaders();
    return this.http.post<any>(`${this.apiUrl}/Events`, eventData, { headers }).pipe(
      map((event: any) => mapApiEventToEvent(event)),
      catchError(this.handleError.bind(this))
    );
  }

  toggleNotifications(): Observable<any> {
  const headers = this.getHeaders();
  return this.http.post(`${this.apiUrl}/User/notifications/toggle`, {}, { headers });
  }

  setNotificationTime(minutes: number): Observable<any> {
    const headers = this.getHeaders();
    return this.http.post(`${this.apiUrl}/User/notifications/time`, minutes, { headers });
  }

  getUserSettings(): Observable<any> {
    const headers = this.getHeaders();
    return this.http.get(`${this.apiUrl}/User/settings`, { headers });
  }

  // Для кураторов
  getCuratorStats(): Observable<any> {
    return this.http.get(`${this.apiUrl}/Curator/stats`, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError.bind(this))
    );
  }

  getGroups(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/Curator/groups`, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError.bind(this))
    );
  }

  getWeekSchedule(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/Curator/week-schedule`, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError.bind(this))
    );
  }

  syncWithJournal(): Observable<any> {
    return this.http.post(`${this.apiUrl}/Curator/sync`, {}, { headers: this.getHeaders() }).pipe(
      catchError(this.handleError.bind(this))
    );
  }
}