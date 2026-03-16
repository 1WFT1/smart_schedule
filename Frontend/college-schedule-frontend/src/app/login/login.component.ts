import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';
import { ChangeDetectorRef } from '@angular/core';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  @Output() loginSuccess = new EventEmitter<void>();
  
  username: string = '';
  password: string = '';
  isLoading: boolean = false;
  errorMessage: string = '';
  showPassword: boolean = false;

  constructor(
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  onSubmit(): void {
    // Сбрасываем состояние перед отправкой
    this.isLoading = true;
    this.errorMessage = '';

    if (!this.username.trim() || !this.password.trim()) {
      this.errorMessage = 'Заполните все поля';
      this.isLoading = false; // ВАЖНО: сбрасываем загрузку
      return;
    }

    // Для администраторов
    if (this.username === 'admin' || this.username.includes('admin')) {
      this.authService.adminLogin(this.username, this.password).subscribe({
        next: (response) => {
          console.log('Успешный вход администратора:', response);
          this.isLoading = false;
          this.loginSuccess.emit();
        },
        error: (error) => {
          console.error('Ошибка входа администратора:', error);
          this.isLoading = false; // ВАЖНО: сбрасываем загрузку
          this.errorMessage = error.message || 'Ошибка при входе';
        }
      });
    } else {
      // Для студентов
      this.authService.studentLogin(this.username, this.password).subscribe({
        next: (response) => {
          console.log('Успешный вход студента:', response);
          this.isLoading = false;
          this.loginSuccess.emit();
        },
        error: (error) => {
          console.error('Ошибка входа студента:', error);
          this.isLoading = false; // ВАЖНО: сбрасываем загрузку
          this.errorMessage = error.message || 'Ошибка при входе';
          
          // Принудительно обновляем представление
          this.cdr?.detectChanges(); // если есть ChangeDetectorRef
        }
      });
    }
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }
}