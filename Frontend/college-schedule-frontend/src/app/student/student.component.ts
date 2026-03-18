import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EventCardComponent } from '../event-card/event-card.component';
import { TimeService } from '../services/time.service';
import { AuthService } from '../services/auth.service';
import { ApiService } from '../services/api.service';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { ScheduleEvent, mapApiEventToEvent } from '../models/event.model';

@Component({
  selector: 'app-student',
  standalone: true,
  imports: [CommonModule, FormsModule, EventCardComponent],
  templateUrl: './student.component.html',
  styleUrls: ['./student.component.css']
})
export class StudentComponent implements OnInit, OnDestroy {
  currentDate: string;
  greeting: string;
  isSyncing: boolean = false;
  syncError: boolean = false;
  isLoading: boolean = false;
  
  // Режим отображения: 'day', 'week' или 'settings'
  viewMode: 'day' | 'week' | 'settings' = 'day';
  
  // Для недельного расписания
  weekDays: Date[] = [];
  weekStartDate: Date;
  weekSchedule: { [key: string]: ScheduleEvent[] } = {};
  
  // Настройки
  settings = {
    notifications: true,
    darkTheme: false,
    notificationTime: 15, // минут до пары
    showGroup: true
  };
  
  userFullName: string = '';
  userGroups: string[] = [];
  
  stats = {
    lectures: 0,
    activities: 0,
    nextTime: '0м'
  };

  activeFilter: string = 'all';
  
  events: ScheduleEvent[] = [];

  private timeSubscription: Subscription;

  constructor(
    private timeService: TimeService,
    private authService: AuthService,
    private apiService: ApiService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {
    const now = new Date();
    this.currentDate = this.formatDate(now);
    
    // Загружаем настройки из localStorage
    this.loadSettings();
    this.applyTheme();
    
    // Устанавливаем начало недели (понедельник)
    this.weekStartDate = new Date(now);
    const day = now.getDay();
    const diff = now.getDay() === 0 ? 6 : now.getDay() - 1;
    this.weekStartDate.setDate(now.getDate() - diff);
    
    this.updateWeekDays();
    
    const user = this.authService.getCurrentUser();
    this.userFullName = user?.fullName || 'Студент';
    this.userGroups = this.authService.getUserGroups();
    
    this.greeting = this.getGreeting();
    
    this.timeSubscription = new Subscription();
  }

  ngOnInit(): void {
    this.loadTodaySchedule();
    this.loadUserSettings(); 
    
    this.timeSubscription = this.timeService.getNextTime().subscribe(time => {
      this.stats.nextTime = time;
    });
    
    setInterval(() => {
      this.updateCurrentEvents();
    }, 60000);
  }

  ngOnDestroy(): void {
    if (this.timeSubscription) {
      this.timeSubscription.unsubscribe();
    }
  }

  // Загрузка настроек из localStorage
  loadUserSettings() {
    // Сначала загружаем из localStorage (тема, группа и т.д.)
    this.loadLocalSettings();
    
    // Затем загружаем с сервера (уведомления)
    this.apiService.getUserSettings().subscribe({
      next: (data) => {
        this.settings.notifications = data.notifications;
        this.settings.notificationTime = data.notificationTime;
        
        // Сохраняем обновленные настройки в localStorage
        localStorage.setItem('userSettings', JSON.stringify(this.settings));
      },
      error: (err) => console.error('Ошибка загрузки настроек с сервера', err)
    });
  }

  private loadLocalSettings(): void {
    const savedSettings = localStorage.getItem('userSettings');
    if (savedSettings) {
      try {
        const localSettings = JSON.parse(savedSettings);
        this.settings = {
          ...this.settings, // сохраняем значения по умолчанию
          ...localSettings, // перезаписываем из localStorage
          // Уведомления пока не трогаем, они придут с сервера
        };
        this.applyTheme();
      } catch (e) {
        console.error('Ошибка загрузки локальных настроек', e);
      }
    }
  }

  // Сохранение настроек
  private saveSettings(): void {
    localStorage.setItem('userSettings', JSON.stringify(this.settings));
    this.applyTheme();
  }

  // Применение темы
  private applyTheme(): void {
    if (this.settings.darkTheme) {
      document.documentElement.classList.add('dark-theme');
      document.body.style.backgroundColor = '#1a202c';
      document.body.style.color = '#f7fafc';
      console.log('Темная тема включена');
    } else {
      document.documentElement.classList.remove('dark-theme');
      document.body.style.backgroundColor = '#f8fafc';
      document.body.style.color = '#1e293b';
      console.log('Светлая тема включена');
    }
  }

  toggleNotifications() {
    this.apiService.toggleNotifications().subscribe({
      next: (response) => {
        this.settings.notifications = response.enabled;
        this.showToast(response.message);
      },
      error: (err) => {
        console.error('Ошибка переключения уведомлений', err);
        this.showToast('❌ Ошибка при изменении настроек', 'error');
      }
    });
  }

  // Переключение темной темы
  toggleDarkTheme(): void {
    this.settings.darkTheme = !this.settings.darkTheme;
    this.saveSettings();
    this.showToast(`Темная тема ${this.settings.darkTheme ? 'включена' : 'выключена'}`);
  }

  // Изменение времени уведомления
  changeNotificationTime(time: number) {
    this.apiService.setNotificationTime(time).subscribe({
      next: (response) => {
        this.settings.notificationTime = response.minutes;
        this.showToast(response.message);
      },
      error: (err) => {
        console.error('Ошибка установки времени', err);
        this.showToast('❌ Ошибка при установке времени', 'error');
      }
    });
  }

  // Переключение отображения группы в заголовке
  toggleShowGroup(): void {
    this.settings.showGroup = !this.settings.showGroup;
    this.saveSettings();
    this.showToast(`Отображение группы ${this.settings.showGroup ? 'включено' : 'выключено'}`);
  }

  goToSettings(): void {
    this.viewMode = 'settings';
    this.updateNavActiveClass('settings');
  }

  goToDay(): void {
    this.viewMode = 'day';
    this.updateNavActiveClass('day');
  }

  goToWeek(): void {
    this.viewMode = 'week';
    this.loadWeekSchedule();
    this.updateNavActiveClass('week');
  }

  private updateNavActiveClass(mode: 'day' | 'week' | 'settings'): void {}

  loadWeekSchedule(): void {
    this.isLoading = true;
    console.log('Загрузка расписания на неделю...');
    
    this.weekSchedule = {};
    
    const promises = this.weekDays.map(day => {
      const dateStr = this.formatDateForApi(day);
      
      return new Promise<void>((resolve) => {
        this.apiService.getDaySchedule(dateStr).subscribe({
          next: (scheduleEvents) => {
            this.apiService.getEvents(dateStr).subscribe({
              next: (extraEvents) => {
                const dateKey = this.formatDateKey(day);
                this.weekSchedule[dateKey] = [...scheduleEvents, ...extraEvents];
                resolve();
              },
              error: () => {
                const dateKey = this.formatDateKey(day);
                this.weekSchedule[dateKey] = scheduleEvents;
                resolve();
              }
            });
          },
          error: () => {
            this.apiService.getEvents(dateStr).subscribe({
              next: (extraEvents) => {
                const dateKey = this.formatDateKey(day);
                this.weekSchedule[dateKey] = extraEvents;
                resolve();
              },
              error: () => {
                const dateKey = this.formatDateKey(day);
                this.weekSchedule[dateKey] = [];
                resolve();
              }
            });
          }
        });
      });
    });
    
    Promise.all(promises).then(() => {
      console.log('Расписание на неделю:', this.weekSchedule);
      this.isLoading = false;
      this.cdr.detectChanges();
    });
  }

  prevWeek(): void {
    this.weekStartDate.setDate(this.weekStartDate.getDate() - 7);
    this.updateWeekDays();
    this.loadWeekSchedule();
  }

  nextWeek(): void {
    this.weekStartDate.setDate(this.weekStartDate.getDate() + 7);
    this.updateWeekDays();
    this.loadWeekSchedule();
  }

  private updateWeekDays(): void {
    this.weekDays = [];
    for (let i = 0; i < 7; i++) {
      const day = new Date(this.weekStartDate);
      day.setDate(this.weekStartDate.getDate() + i);
      this.weekDays.push(day);
    }
  }

  getDayName(date: Date): string {
    const days = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс'];
    return days[date.getDay() === 0 ? 6 : date.getDay() - 1];
  }

  getDayNumber(date: Date): string {
    return date.getDate().toString();
  }

  getMonthName(date: Date): string {
    const months = ['янв', 'фев', 'мар', 'апр', 'май', 'июн', 'июл', 'авг', 'сен', 'окт', 'ноя', 'дек'];
    return months[date.getMonth()];
  }

  isToday(date: Date): boolean {
    const today = new Date();
    return date.getDate() === today.getDate() &&
           date.getMonth() === today.getMonth() &&
           date.getFullYear() === today.getFullYear();
  }

  private formatDateKey(date: Date): string {
    return `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, '0')}-${date.getDate().toString().padStart(2, '0')}`;
  }

  private formatDateForApi(date: Date): string {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  getEventsForDay(date: Date): ScheduleEvent[] {
    const dateKey = this.formatDateKey(date);
    return this.weekSchedule[dateKey] || [];
  }

  getWeekRange(): string {
    const start = this.weekDays[0];
    const end = this.weekDays[6];
    
    const startStr = `${start.getDate()} ${this.getMonthName(start)}`;
    const endStr = `${end.getDate()} ${this.getMonthName(end)} ${end.getFullYear()}`;
    
    if (start.getMonth() === end.getMonth()) {
      return `${start.getDate()} – ${end.getDate()} ${this.getMonthName(end)} ${end.getFullYear()}`;
    } else {
      return `${start.getDate()} ${this.getMonthName(start)} – ${end.getDate()} ${this.getMonthName(end)} ${end.getFullYear()}`;
    }
  }

  loadTodaySchedule(): void {
    this.isLoading = true;
    console.log('Загрузка расписания...');
    
    this.apiService.getTodaySchedule().subscribe({
      next: (scheduleEvents) => {
        console.log('Расписание из журнала получено:', scheduleEvents);
        
        this.apiService.getEvents().subscribe({
          next: (extraEvents) => {
            console.log('Мероприятия из БД получены:', extraEvents);
            this.events = [...scheduleEvents, ...extraEvents];
            this.processEvents();
          },
          error: (error) => {
            console.error('Ошибка загрузки мероприятий:', error);
            this.events = [...scheduleEvents];
            this.processEvents();
          }
        });
      },
      error: (error) => {
        console.error('Ошибка загрузки расписания:', error);
        
        this.apiService.getEvents().subscribe({
          next: (extraEvents) => {
            console.log('Только мероприятия:', extraEvents);
            this.events = [...extraEvents];
            this.processEvents();
          },
          error: (err) => {
            console.error('Полная ошибка загрузки:', err);
            this.events = [];
            this.isLoading = false;
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  private processEvents(): void {
    this.events.sort((a, b) => {
      const timeA = a.startTime || '00:00';
      const timeB = b.startTime || '00:00';
      return timeA.localeCompare(timeB);
    });
    
    console.log('Итоговые события:', this.events);
    
    this.updateCurrentEvents();
    this.saveEventsToStorage();
    this.timeService.calculateNextTime(this.events);
    this.isLoading = false;
    this.cdr.detectChanges();
  }

  filterEvents(type: string): void {
    this.activeFilter = type;
  }

  get filteredEvents(): ScheduleEvent[] {
    if (!this.events || this.events.length === 0) {
      return [];
    }
    
    if (this.activeFilter === 'all') {
      return this.events;
    }
    
    return this.events.filter(event => event.category === this.activeFilter);
  }

  syncData(): void {
    if (this.isSyncing) return;
    
    this.isSyncing = true;
    this.syncError = false;
    
    setTimeout(() => {
      this.isSyncing = false;
      const success = Math.random() > 0.2;
      if (success) {
        this.loadTodaySchedule();
        this.showToast('Расписание синхронизировано!');
      } else {
        this.syncError = true;
        this.showToast('Ошибка синхронизации', 'error');
      }
    }, 1500);
  }

  logout(): void {
    if (confirm('Вы уверены, что хотите выйти?')) {
      this.authService.logout();
    }
  }

  private saveEventsToStorage(): void {
    localStorage.setItem('scheduleEvents', JSON.stringify(this.events));
  }

  private updateCurrentEvents(): void {
    this.events.forEach(event => {
      if (event.time) {
        event.isCurrent = this.timeService.isEventCurrent(event.time);
        if (event.isCurrent) {
          event.timeRemaining = this.timeService.getTimeRemainingForEvent(event.time);
          
          const hasCurrentTag = event.tags.includes('Текущая');
          if (!hasCurrentTag) {
            event.tags = ['Текущая', ...event.tags.filter(tag => tag !== 'Текущая')];
          }
        } else {
          event.timeRemaining = undefined;
          event.tags = event.tags.filter(tag => tag !== 'Текущая');
        }
      }
    });
    
    const studyEvents = this.events.filter(e => e.category === 'study');
    const extraEvents = this.events.filter(e => e.category === 'extra');
    
    this.stats.lectures = studyEvents.length;
    this.stats.activities = extraEvents.length;
    
    this.timeService.calculateNextTime(this.events);
  }

  private formatDate(date: Date): string {
    const days = ['Воскресенье', 'Понедельник', 'Вторник', 'Среда', 'Четверг', 'Пятница', 'Суббота'];
    const months = ['января', 'февраля', 'марта', 'апреля', 'мая', 'июня', 'июля', 'августа', 'сентября', 'октября', 'ноября', 'декабря'];
    return `${days[date.getDay()]}, ${date.getDate()} ${months[date.getMonth()]}`;
  }

  private getGreeting(): string {
    const hour = new Date().getHours();
    const name = this.userFullName.split(' ')[1] || 'Студент';
    if (hour < 6) return `Доброй ночи, ${name}!`;
    if (hour < 12) return `Доброе утро, ${name}!`;
    if (hour < 18) return `Добрый день, ${name}!`;
    return `Добрый вечер, ${name}!`;
  }

  private showToast(message: string, type: 'success' | 'error' = 'success'): void {
    console.log(`%c${message}`, `color: ${type === 'success' ? 'green' : 'red'}; font-weight: bold;`);
    alert(message);
  }
}