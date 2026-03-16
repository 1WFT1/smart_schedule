import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../services/api.service';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-curator-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './curator-panel.component.html',
  styleUrls: ['./curator-panel.component.css']
})
export class CuratorPanelComponent implements OnInit {
  curatorName: string = '';
  groups: string[] = [];
  allGroups: any[] = [
    { id: 'all', name: 'Все группы', count: 24 },
    { id: 'group1', name: 'ПИ-21-1', count: 12 },
    { id: 'group2', name: 'ПИ-21-2', count: 12 },
    { id: 'group3', name: '9\\4-РПО-22\\2-39', count: 8 }
  ];
  selectedGroup: string = 'all';
  
  stats = {
    totalLessons: 42,
    totalEvents: 8,
    freeRooms: 14
  };
  
  weekRange: string = '';
  weekSchedule: any[] = [
    {
      subject: 'Программирование',
      room: '404',
      teacher: 'Иванов',
      type: 'lecture',
      dayIndex: 1,
      timeSlot: 2
    },
    {
      subject: 'Собрание',
      room: 'Актовый зал',
      teacher: 'Куратор',
      type: 'event',
      dayIndex: 2,
      timeSlot: 3
    }
  ];
  
  isSyncing: boolean = false;
  lastSync: string = '';
  
  // Modal state
  showQuickAddModal: boolean = false;
  
  // New event form
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

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadCuratorData();
    this.updateWeekRange();
  }

  loadCuratorData() {
    const user = this.authService.getCurrentUser();
    this.curatorName = user?.fullName || 'Тестовый Администратор';
    
    // Получаем группы куратора
    if (user?.role === 'admin') {
      // Для админа показываем все группы, включая вашу
      this.groups = ['ПИ-21-1', 'ПИ-21-2', '9/4-РПО-22/2-39'];
    } else {
      // Для куратора берем его группы из БД
      this.groups = user?.groups || ['ПИ-21-1', 'ПИ-21-2'];
    }
    
    // Загружаем статистику
    this.apiService.getCuratorStats().subscribe({
      next: (data) => {
        this.stats = data;
      },
      error: (err) => console.error('Ошибка загрузки статистики', err)
    });
  }
  updateWeekRange() {
    const start = new Date();
    const end = new Date();
    end.setDate(end.getDate() + 6);
    
    const startStr = start.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' });
    const endStr = end.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' });
    
    this.weekRange = `${startStr} – ${endStr}`;
  }

  filterByGroup(groupId: string) {
    this.selectedGroup = groupId;
    // Здесь будет логика фильтрации расписания
    console.log('Filter by group:', groupId);
  }

  changeWeek(direction: number) {
    console.log('Change week:', direction);
    // Здесь будет логика смены недели
  }

  // Quick Add methods
  openQuickAdd() {
    this.showQuickAddModal = true;
    // Устанавливаем сегодняшнюю дату по умолчанию
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

    // Парсим дату и время
    const [year, month, day] = this.newEvent.date.split('-').map(Number);
    const [hours, minutes] = this.newEvent.time.split(':').map(Number);
    
    const startDateTime = new Date(year, month - 1, day, hours, minutes);
    const endDateTime = new Date(startDateTime);
    endDateTime.setHours(startDateTime.getHours() + Math.floor(this.newEvent.duration));
    endDateTime.setMinutes(startDateTime.getMinutes() + (this.newEvent.duration % 1) * 60);

    // Формируем теги
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

    console.log('Creating event:', eventData);

    this.apiService.createEvent(eventData).subscribe({
      next: (response) => {
        console.log('Event created:', response);
        this.closeQuickAdd();
        // Показываем уведомление
        alert('Событие успешно создано!');
      },
      error: (err) => {
        console.error('Error creating event:', err);
        alert('Ошибка при создании события');
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

  // Other actions
  openMassEdit() {
    console.log('Mass edit');
  }

  importFromExcel() {
    console.log('Import from Excel');
    alert('Импорт из Excel будет доступен позже');
  }

  clearDay() {
    if (confirm('Вы уверены, что хотите очистить расписание на день?')) {
      console.log('Clear day');
    }
  }

  duplicateWeek() {
    console.log('Duplicate week');
    alert('Дублирование недели будет доступно позже');
  }

  publishChanges() {
    console.log('Publish');
    alert('Изменения опубликованы');
  }

  syncWithJournal() {
    this.isSyncing = true;
    this.apiService.syncWithJournal().subscribe({
      next: (res) => {
        this.lastSync = new Date().toLocaleTimeString();
        this.isSyncing = false;
        alert('Синхронизация завершена успешно!');
      },
      error: () => {
        this.isSyncing = false;
        alert('Ошибка синхронизации');
      }
    });
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}