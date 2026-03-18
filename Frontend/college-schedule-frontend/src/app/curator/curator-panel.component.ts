import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../services/api.service';
import { AuthService } from '../services/auth.service';
import { GroupService } from '../services/group.service';
import { Group } from '../models/group.model';
import { GroupManagementComponent } from './group-management/group-management.component';
import { ScheduleEvent } from '../models/event.model';

@Component({
  selector: 'app-curator-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, GroupManagementComponent],
  templateUrl: './curator-panel.component.html',
  styleUrls: ['./curator-panel.component.css']
})
export class CuratorPanelComponent implements OnInit {
  curatorName: string = '';
  groups: Group[] = [];
  selectedGroup: string = '';
  
  weekRange: string = '';
  weekDays: Date[] = [];
  weekStartDate: Date;
  weekSchedule: { [key: string]: ScheduleEvent[] } = {};
  
  showQuickAddModal: boolean = false;
  
  newEvent = {
    title: '',
    type: 'extra' as 'study' | 'extra',
    date: '',
    time: '',
    duration: 1.5,
    teacher: '',
    room: '',
    tagsInput: '',
    selectedGroups: [] as string[]
  };

  showGroupManagement: boolean = false;
  isRefreshing: boolean = false;
  isLoading: boolean = false;

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
    private groupService: GroupService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    const now = new Date();
    this.weekStartDate = new Date(now);
    const day = now.getDay();
    const diff = now.getDay() === 0 ? 6 : now.getDay() - 1;
    this.weekStartDate.setDate(now.getDate() - diff);
    this.updateWeekDays();
  }

  ngOnInit() {
    this.loadCuratorData();
    this.loadGroups();
    this.updateWeekRange();
    
    this.groupService.groups$.subscribe(groups => {
      console.log('groups$ в панели:', groups);
      this.groups = groups;
      this.cdr.detectChanges();
    });
  }

  loadCuratorData() {
    const user = this.authService.getCurrentUser();
    this.curatorName = user?.fullName || 'Куратор';
  }

  loadGroups() {
    this.isRefreshing = true;
    this.groupService.getGroups().subscribe({
      next: (groups) => {
        console.log('loadGroups получил:', groups);
        this.groups = groups;
        this.isRefreshing = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Ошибка загрузки групп', err);
        this.isRefreshing = false;
      }
    });
  }

  refreshGroups() {
    this.loadGroups();
  }

  toggleGroupManagement() {
    this.showGroupManagement = !this.showGroupManagement;
  }

  getActiveGroupsCount(): number {
    return this.groups.filter(g => g.isActive).length;
  }

  filterByGroup(groupName: string) {
    this.selectedGroup = groupName;
    this.loadWeekSchedule();
  }

  loadWeekSchedule() {
    if (!this.selectedGroup) return;
    
    this.isLoading = true;
    const startDate = this.formatDateForApi(this.weekStartDate);
    
    console.log(`Загрузка расписания для группы ${this.selectedGroup} с ${startDate}`);
    
    this.apiService.getGroupWeekSchedule(this.selectedGroup, startDate).subscribe({
      next: (schedule) => {
        console.log('Расписание получено:', schedule);
        
        // Фильтруем только текущую неделю
        const filteredSchedule: { [key: string]: ScheduleEvent[] } = {};
        const weekDates: string[] = this.weekDays.map(d => this.formatDateKey(d));
        
        Object.keys(schedule || {}).forEach(dateKey => {
          if (weekDates.includes(dateKey)) {
            filteredSchedule[dateKey] = schedule[dateKey];
          }
        });
        
        this.weekSchedule = filteredSchedule;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Ошибка загрузки расписания:', error);
        this.isLoading = false;
      }
    });
  }

  // Получение времени события для отображения
  getEventTime(event: ScheduleEvent): string {
    if (event.time) return event.time;
    
    const start = this.getLocalTimeFromUTC(event.startTime || '');
    const end = this.getLocalTimeFromUTC(event.endTime || event.startTime || '');
    return start && end ? `${start} – ${end}` : '';
  }

  // Группировка событий по времени начала
  getEventsGroupedByTime(day: Date): { [time: string]: ScheduleEvent[] } {
    const dateKey = this.formatDateKey(day);
    const dayEvents = this.weekSchedule[dateKey] || [];
    
    const grouped: { [time: string]: ScheduleEvent[] } = {};
    
    dayEvents.forEach(event => {
      if (!event.startTime) return;
      
      const localTime = this.getLocalTimeFromUTC(event.startTime);
      
      if (!grouped[localTime]) {
        grouped[localTime] = [];
      }
      grouped[localTime].push(event);
    });
    
    return grouped;
  }

  // Получение локального времени из UTC
  getLocalTimeFromUTC(utcTimeStr: string): string {
    if (!utcTimeStr) return '';
    
    // Берем время из строки "2026-03-18T08:20:00" -> "08:20"
    const timePart = utcTimeStr.split('T')[1]?.substring(0, 5);
    return timePart || '';
  }

  // Получение всех уникальных временных слотов
  getAllTimeSlots(): string[] {
    const allTimes = new Set<string>();
    
    this.weekDays.forEach(day => {
      const grouped = this.getEventsGroupedByTime(day);
      Object.keys(grouped).forEach(time => allTimes.add(time));
    });
    
    return Array.from(allTimes).sort((a, b) => {
      const [hourA, minA] = a.split(':').map(Number);
      const [hourB, minB] = b.split(':').map(Number);
      return (hourA * 60 + minA) - (hourB * 60 + minB);
    });
  }

  // Получение событий для конкретного времени
  getEventsAtTime(day: Date, time: string): ScheduleEvent[] {
    const grouped = this.getEventsGroupedByTime(day);
    return grouped[time] || [];
  }

  getEventsForDay(date: Date): ScheduleEvent[] {
    const dateKey = this.formatDateKey(date);
    return this.weekSchedule[dateKey] || [];
  }

  changeWeek(direction: number) {
    const newDate = new Date(this.weekStartDate);
    newDate.setDate(this.weekStartDate.getDate() + direction * 7);
    this.weekStartDate = newDate;
    
    this.updateWeekDays();
    this.updateWeekRange();
    
    if (this.selectedGroup) {
      this.loadWeekSchedule();
    }
  }

  private updateWeekDays(): void {
    this.weekDays = [];
    for (let i = 0; i < 7; i++) {
      const day = new Date(this.weekStartDate);
      day.setDate(this.weekStartDate.getDate() + i);
      this.weekDays.push(day);
    }
  }

  updateWeekRange() {
    const start = this.weekDays[0];
    const end = this.weekDays[6];
    
    const startStr = start.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' });
    const endStr = end.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' });
    
    this.weekRange = `${startStr} – ${endStr}`;
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

  formatDateKey(date: Date): string {
    return `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, '0')}-${date.getDate().toString().padStart(2, '0')}`;
  }

  formatDateForApi(date: Date): string {
    return this.formatDateKey(date);
  }

  openQuickAdd() {
    this.showQuickAddModal = true;
    const today = new Date();
    this.newEvent.date = today.toISOString().split('T')[0];
    this.newEvent.time = '10:00';
  }

  closeQuickAdd() {
    this.showQuickAddModal = false;
    this.resetNewEvent();
  }

  toggleGroup(groupName: string) {
    const index = this.newEvent.selectedGroups.indexOf(groupName);
    if (index === -1) {
      this.newEvent.selectedGroups.push(groupName);
    } else {
      this.newEvent.selectedGroups.splice(index, 1);
    }
  }

  isEventValid(): boolean {
    return !!(
      this.newEvent.title && 
      this.newEvent.date && 
      this.newEvent.time && 
      this.newEvent.selectedGroups.length > 0
    );
  }

  createEvent() {
    if (!this.isEventValid()) return;

    const [year, month, day] = this.newEvent.date.split('-').map(Number);
    const [hours, minutes] = this.newEvent.time.split(':').map(Number);
    
    const startDateTime = new Date(year, month - 1, day, hours, minutes);
    const endDateTime = new Date(startDateTime);
    endDateTime.setHours(startDateTime.getHours() + this.newEvent.duration);

    const tags = this.newEvent.tagsInput
      .split(',')
      .map(tag => tag.trim())
      .filter(tag => tag);

    const eventData = {
      type: this.newEvent.type,
      category: this.newEvent.type,
      name: this.newEvent.title,
      teacher: this.newEvent.teacher || 'Куратор',
      room: this.newEvent.room || 'Не указано',
      group: this.newEvent.selectedGroups.join(', '),
      startTime: startDateTime.toISOString(),
      endTime: endDateTime.toISOString(),
      tags: tags.length ? tags : ['Новое'],
      targetGroups: this.newEvent.selectedGroups
    };

    this.apiService.createEvent(eventData).subscribe({
      next: () => {
        this.closeQuickAdd();
        alert('✅ Событие создано!');
        if (this.selectedGroup) {
          this.loadWeekSchedule();
        }
      },
      error: (err) => {
        console.error('Ошибка:', err);
        alert('❌ Ошибка при создании события');
      }
    });
  }

  resetNewEvent() {
    this.newEvent = {
      title: '',
      type: 'extra',
      date: '',
      time: '',
      duration: 1.5,
      teacher: '',
      room: '',
      tagsInput: '',
      selectedGroups: []
    };
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}