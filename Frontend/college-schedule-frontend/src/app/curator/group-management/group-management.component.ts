// src/app/curator/group-management/group-management.component.ts
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GroupService } from '../../services/group.service';
import { Group, CreateGroupDto } from '../../models/group.model';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';

@Component({
  selector: 'app-group-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './group-management.component.html',
  styleUrls: ['./group-management.component.css']
})
export class GroupManagementComponent implements OnInit {
  groups: Group[] = [];
  filteredGroups: Group[] = [];
  
  showAddForm = false;
  newGroupName = '';
  newGroupDisplayName = '';
  
  searchQuery = '';
  filterSource: 'all' | 'manual' | 'journal' = 'all';
  
  isLoading = false;
  errorMessage = '';

  constructor(
    private groupService: GroupService,
    private cdr: ChangeDetectorRef  // Внедряем ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadGroups();
  }

  loadGroups(): void {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.groupService.getGroups().subscribe({
      next: (groups) => {
        this.groups = groups;
        this.applyFilters();
        this.isLoading = false;
        
        // ПРИНУДИТЕЛЬНО ОБНОВЛЯЕМ ВЬЮ
        this.cdr.detectChanges();
        
        console.log('Группы загружены:', groups);
      },
      error: (err) => {
        this.errorMessage = err.message || 'Ошибка загрузки групп';
        this.isLoading = false;
        this.cdr.detectChanges(); // Обновляем и при ошибке
        console.error('Ошибка загрузки групп', err);
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.groups];
    
    // Поиск по названию
    if (this.searchQuery.trim()) {
      const query = this.searchQuery.toLowerCase();
      filtered = filtered.filter(g => 
        g.name.toLowerCase().includes(query) ||
        g.displayName.toLowerCase().includes(query)
      );
    }
    
    // Фильтр по источнику
    if (this.filterSource !== 'all') {
      filtered = filtered.filter(g => g.source === this.filterSource);
    }
    
    this.filteredGroups = filtered;
  }

  openAddForm(): void {
    this.showAddForm = true;
    this.newGroupName = '';
    this.newGroupDisplayName = '';
  }

  cancelAdd(): void {
    this.showAddForm = false;
  }

createGroup(): void {
  if (!this.newGroupName.trim()) {
    alert('Введите название группы');
    return;
  }

  // Проверяем, есть ли уже группа с таким именем
  const exists = this.groups.some(g => 
    g.name.toLowerCase() === this.newGroupName.trim().toLowerCase()
  );

  if (exists) {
    alert('Группа с таким названием уже существует');
    return;
  }

  const groupData = { 
    name: this.newGroupName.trim() 
  };

  console.log('Отправляем данные:', groupData);

  this.isLoading = true;
  this.groupService.createGroup(groupData).subscribe({
    next: (newGroup) => {
      console.log('Группа создана:', newGroup);
      this.groups.push(newGroup);
      this.applyFilters();
      this.showAddForm = false;
      this.isLoading = false;
      this.newGroupName = '';
      this.newGroupDisplayName = '';
    },
    error: (err) => {
      console.error('Ошибка создания группы:', err);
      
      // Если ошибка 400, показываем понятное сообщение
      if (err.status === 400) {
        this.errorMessage = 'Группа с таким названием уже существует';
      } else {
        this.errorMessage = err.message || 'Ошибка создания группы';
      }
      
      this.isLoading = false;
    }
  });
}

  editGroup(group: Group): void {
    const newName = prompt('Введите новое название группы:', group.displayName);
    if (newName && newName.trim()) {
      this.isLoading = true;
      this.groupService.updateGroup(group.id, {
        displayName: newName.trim(),
        name: newName.trim()
      }).subscribe({
        next: () => {
          group.displayName = newName.trim();
          group.name = newName.trim();
          this.applyFilters();
          this.isLoading = false;
        },
        error: (err) => {
          this.errorMessage = err.message || 'Ошибка обновления группы';
          this.isLoading = false;
        }
      });
    }
  }

  deleteGroup(group: Group): void {
    if (confirm(`Удалить группу "${group.displayName}"?`)) {
      this.isLoading = true;
      this.groupService.deleteGroup(group.id).subscribe({
        next: () => {
          this.groups = this.groups.filter(g => g.id !== group.id);
          this.applyFilters();
          this.isLoading = false;
        },
        error: (err) => {
          this.errorMessage = err.message || 'Ошибка удаления группы';
          this.isLoading = false;
        }
      });
    }
  }

  toggleGroupStatus(group: Group): void {
    this.isLoading = true;
    this.groupService.updateGroup(group.id, {
      isActive: !group.isActive
    }).subscribe({
      next: () => {
        group.isActive = !group.isActive;
        this.applyFilters();
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message || 'Ошибка изменения статуса';
        this.isLoading = false;
      }
    });
  }

  viewGroupStudents(group: Group): void {
    this.groupService.getGroupStudents(group.id).subscribe({
      next: (students) => {
        alert(`В группе ${students.length} студентов`);
        console.log('Студенты:', students);
      },
      error: (err) => {
        alert('Ошибка загрузки студентов');
      }
    });
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  getSourceLabel(source: string): string {
    return source === 'manual' ? 'Ручное' : 'Из журнала';
  }

  getStatusClass(isActive: boolean): string {
    return isActive ? 'badge-active' : 'badge-inactive';
  }

  getStatusText(isActive: boolean): string {
    return isActive ? 'Активна' : 'Неактивна';
  }
}