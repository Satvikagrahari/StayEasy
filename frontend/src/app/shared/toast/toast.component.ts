import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Toast, ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.css'
})
export class ToastComponent {
  toastService = inject(ToastService);

  iconFor(type: Toast['type']): string {
    return {
      success: '✓',
      error: '✕',
      warning: '⚠',
      info: 'ℹ'
    }[type];
  }
}
