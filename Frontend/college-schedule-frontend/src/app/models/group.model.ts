export interface Group {
  id: string;              // уникальный ID (можно использовать имя группы)
  name: string;            // "9/4-РПО-22/2-39"
  displayName: string;     // для отображения (можно такое же имя)
  studentCount?: number;   // количество студентов (будем обновлять)
  createdAt: Date;         // когда добавлена
  lastActive?: Date;       // последний вход студента
  source: 'manual' | 'journal'; // создана вручную или из журнала
  isActive: boolean;       // активна ли группа
}

// Для хранения в localStorage
export interface GroupsData {
  groups: Group[];
  lastUpdated: Date;
}