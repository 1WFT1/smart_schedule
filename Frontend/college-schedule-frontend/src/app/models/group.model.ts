// src/app/models/group.model.ts

export interface Group {
  id: number;              // В БД это int, а не string!
  name: string;            // "9/4-РПО-22/2-39"
  displayName: string;     // для отображения
  studentCount: number;    // количество студентов
  createdAt: string;       // ISO string (Date приходит как строка с бекенда)
  lastActive?: string;     // ISO string или null
  source: 'manual' | 'journal';
  isActive: boolean;
  curatorId?: number;      // ID куратора (если есть)
  curatorName?: string;    // Имя куратора для отображения
}

// Для ответа от API (GroupsController)
export interface CreateGroupDto {
  name: string;
  displayName?: string;
  studentCount?: number;
  source?: 'manual' | 'journal';
}

export interface UpdateGroupDto {
  name?: string;
  displayName?: string;
  studentCount?: number;
  isActive?: boolean;
}