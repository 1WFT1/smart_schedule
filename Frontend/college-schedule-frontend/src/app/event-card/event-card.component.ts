import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ScheduleEvent  } from '../models/event.model';

@Component({
  selector: 'app-event-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="event-card" [ngClass]="[event.category, event.type]">
      <div class="event-type" [ngClass]="'type-' + event.type">
        {{ getTypeLabel(event.type) }}
      </div>
      <div class="event-time">
        <i class="far fa-clock"></i> {{ event.time }}
        <span *ngIf="event.timeRemaining" class="time-remaining">• {{ event.timeRemaining }}</span>
      </div>
      <div class="event-name">{{ event.name }}</div>
      <div class="event-details">
        <span *ngIf="event.teacher">
          <i class="fas fa-chalkboard-teacher"></i> {{ event.teacher }}
        </span>
        <span *ngIf="event.room">
          <i class="fas fa-door-open"></i> {{ event.room }}
        </span>
        <span *ngIf="event.group">
          <i class="fas fa-users"></i> {{ event.group }}
        </span>
      </div>
      <div class="event-tags">
        <div class="tag" *ngFor="let tag of event.tags" 
            [ngClass]="{'important': tag === 'Зачёт' || tag === 'Текущая', 
                        'online': tag === 'Можно онлайн' || tag === 'Трансляция'}">
          {{ tag }}
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./event-card.component.css']
})
export class EventCardComponent {
  @Input() event!: ScheduleEvent;

  ngOnInit() {
    console.log('EventCardComponent получил событие:', this.event);
    console.log('Имя события:', this.event?.name);
    console.log('Время:', this.event?.time);
  }
  
  getTypeLabel(type: string): string {
    const labels: { [key: string]: string } = {
      lecture: 'Лекция',
      practice: 'Практика',
      extra: 'Доп. занятие',
      activity: 'Мероприятие'
    };
    return labels[type] || type;
  }
}