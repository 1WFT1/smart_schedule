export interface ScheduleEvent {
  id: number;
  type: 'lecture' | 'practice' | 'extra' | 'activity';
  category: 'study' | 'extra';
  time: string;              // "14:40 - 16:00"
  name: string;
  teacher?: string;          // вместо details
  room?: string;             // вместо details  
  group?: string;            // вместо details
  tags: string[];
  isCurrent?: boolean;
  timeRemaining?: string;
  startTime?: string;
  endTime?: string;
}

// Функция для конвертации из API в модель
export function mapApiEventToEvent(apiEvent: any): ScheduleEvent  {
  console.log('Конвертация API события:', apiEvent);
  
  // Форматируем время из ISO строк в "14:40 - 16:00"
  const formatTime = (isoString: string) => {
    if (!isoString) return '';
    const date = new Date(isoString);
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    return `${hours}:${minutes}`;
  };
  
  const startFormatted = formatTime(apiEvent.startTime);
  const endFormatted = formatTime(apiEvent.endTime);
  const timeString = startFormatted && endFormatted ? `${startFormatted} – ${endFormatted}` : '';
  
  return {
    id: apiEvent.id,
    type: apiEvent.type,
    category: apiEvent.category,
    time: timeString, // Теперь время будет "14:40 – 16:00"
    name: apiEvent.name,
    teacher: apiEvent.teacher,
    room: apiEvent.room,
    group: apiEvent.group,
    tags: apiEvent.tags || [],
    isCurrent: apiEvent.isCurrent,
    timeRemaining: apiEvent.timeRemaining,
    startTime: apiEvent.startTime,
    endTime: apiEvent.endTime
  };
}