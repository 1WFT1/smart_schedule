import { Injectable } from '@angular/core';
import { interval, Observable, BehaviorSubject } from 'rxjs';
import { startWith } from 'rxjs/operators';
import { ScheduleEvent } from '../models/event.model';

@Injectable({
  providedIn: 'root'
})
export class TimeService {
  private nextTimeSubject = new BehaviorSubject<string>('40м');

  constructor() {
    // Запускаем обновление каждую минуту
    interval(60000).pipe(
      startWith(0)
    ).subscribe(() => {
      // Обновляем время (события будут переданы из компонента)
      const events = this.getStoredEvents();
      this.calculateNextTime(events);
    });
  }

  // Метод для расчета времени до следующей пары
  calculateNextTime(events: ScheduleEvent[]): void {
  const now = new Date();
  const currentTime = now.getHours() * 60 + now.getMinutes();
  
  console.log('Текущее время:', currentTime);
  
  // Получаем все учебные события
  const studyEvents = events.filter(e => e.category === 'study');
  
  console.log('Учебные события:', studyEvents.map(e => ({
    name: e.name,
    time: e.time,
    startTime: e.startTime
  })));
  
  let minTimeDiff = Infinity;
  
  // Ищем ближайшую будущую пару
  for (const event of studyEvents) {
    // Пробуем получить время начала разными способами
    let timeStr = '';
    
    if (event.startTime) {
      // Если есть startTime в формате ISO
      try {
        const date = new Date(event.startTime);
        if (!isNaN(date.getTime())) {
          timeStr = `${date.getHours()}:${date.getMinutes().toString().padStart(2, '0')}`;
        }
      } catch (e) {
        // Игнорируем
      }
    }
    
    // Если не получили из startTime, пробуем из поля time
    if (!timeStr && event.time) {
      const timeParts = event.time.split(' – ');
      if (timeParts.length > 0) {
        timeStr = timeParts[0];
      }
    }
    
    if (!timeStr) continue;
    
    console.log(`Событие ${event.name}, время начала: ${timeStr}`);
    
    const [hours, minutes] = timeStr.split(':').map(Number);
    if (isNaN(hours) || isNaN(minutes)) continue;
    
    const eventStartTime = hours * 60 + minutes;
    
    console.log(`Событие ${event.name}: начало в ${eventStartTime}, сейчас ${currentTime}`);
    
    // Если пара еще не началась
    if (eventStartTime > currentTime) {
      const timeDiff = eventStartTime - currentTime;
      console.log(`До ${event.name}: ${timeDiff} минут`);
      if (timeDiff < minTimeDiff) {
        minTimeDiff = timeDiff;
      }
    }
  }
  
    // Форматируем время
    let timeString: string;
    
    if (minTimeDiff === Infinity) {
      // Нет будущих пар
      if (studyEvents.length === 0) {
        timeString = 'Нет пар';
      } else {
        timeString = 'Все пары прошли';
      }
    } else {
      // Есть будущая пара
      if (minTimeDiff >= 60) {
        const hours = Math.floor(minTimeDiff / 60);
        const minutes = minTimeDiff % 60;
        timeString = minutes > 0 ? `${hours}ч ${minutes}м` : `${hours}ч`;
      } else {
        timeString = `${minTimeDiff}м`;
      }
    }
    
    console.log('Результат:', timeString);
    this.nextTimeSubject.next(timeString);
  }



  getNextTime(): Observable<string> {
    return this.nextTimeSubject.asObservable();
  }

  

  getTimeRemainingForEvent(eventTime: string): string {
    const [startStr, endStr] = eventTime.split(' – ');
    const [hours, minutes] = startStr.split(':').map(Number);
    const [endHours, endMinutes] = endStr.split(':').map(Number);
    
    const now = new Date();
    const currentTime = now.getHours() * 60 + now.getMinutes();
    const eventStartTime = hours * 60 + minutes;
    const eventEndTime = endHours * 60 + endMinutes;
    
    // Если событие уже началось
    if (currentTime >= eventStartTime && currentTime <= eventEndTime) {
      const remainingTime = eventEndTime - currentTime;
      
      if (remainingTime > 60) {
        const hoursRemaining = Math.floor(remainingTime / 60);
        const minutesRemaining = remainingTime % 60;
        return `Осталось ${hoursRemaining}ч ${minutesRemaining}м`;
      } else {
        return `Осталось ${remainingTime}м`;
      }
    }
    
    return '';
  }

  isEventCurrent(eventTime: string): boolean {
    const [startStr, endStr] = eventTime.split(' – ');
    const [startHours, startMinutes] = startStr.split(':').map(Number);
    const [endHours, endMinutes] = endStr.split(':').map(Number);
    
    const now = new Date();
    const currentTime = now.getHours() * 60 + now.getMinutes();
    const eventStartTime = startHours * 60 + startMinutes;
    const eventEndTime = endHours * 60 + endMinutes;
    
    return currentTime >= eventStartTime && currentTime <= eventEndTime;
  }

  // Вспомогательный метод для хранения событий
  private getStoredEvents(): any[] {
    return JSON.parse(localStorage.getItem('scheduleEvents') || '[]');
  }
}