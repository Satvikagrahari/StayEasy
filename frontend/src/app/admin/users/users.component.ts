import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../core/services/admin-api.service';
import { ToastService } from '../../core/services/toast.service';
import { UserProfile } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css'
})
export class UsersComponent implements OnInit {
  private adminApi = inject(AdminApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);

  users = signal<UserProfile[]>([]);
  isLoading = signal(true);
  error = signal<string | null>(null);
  actionId = signal<string | null>(null);
  deleteTarget = signal<UserProfile | null>(null);
  toggleTarget = signal<UserProfile | null>(null);
  search = '';
  palette = ['#2563EB', '#0EA5E9', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#14B8A6', '#F97316'];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true);
    this.adminApi.getAllUsers().subscribe({
      next: (u) => { this.users.set(u); this.isLoading.set(false); },
      error: () => { this.error.set('Failed to load users.'); this.isLoading.set(false); }
    });
  }

  toggle(user: UserProfile): void {
    this.actionId.set(user.id);
    const op = user.isActive ? this.adminApi.deactivateUser(user.id) : this.adminApi.activateUser(user.id);
    op.subscribe({
      next: () => {
        this.actionId.set(null);
        this.toast.success(user.isActive ? 'User deactivated.' : 'User activated.');
        this.load();
      },
      error: (err) => { this.actionId.set(null); this.toast.error(err.error?.message ?? 'Action failed.'); }
    });
  }

  delete(id: string): void {
    this.actionId.set(id);
    this.adminApi.deleteUser(id).subscribe({
      next: () => { this.actionId.set(null); this.toast.success('User deleted.'); this.load(); },
      error: (err) => { this.actionId.set(null); this.toast.error(err.error?.message ?? 'Delete failed.'); }
    });
  }

  confirmDelete(): void {
    const target = this.deleteTarget();
    if (!target) return;
    this.deleteTarget.set(null);
    this.delete(target.id);
  }

  confirmToggle(): void {
    const target = this.toggleTarget();
    if (!target) return;
    this.toggleTarget.set(null);
    this.toggle(target);
  }

  filteredUsers(): UserProfile[] {
    const q = this.search.trim().toLowerCase();
    return this.users().filter(u => !q || u.userName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q));
  }

  isSelf(user: UserProfile): boolean {
    return user.userName?.toLowerCase() === this.auth.getCurrentUser()?.username?.toLowerCase();
  }

  initials(user: UserProfile): string {
    return (user.userName || user.email || 'US').slice(0, 2).toUpperCase();
  }

  avatarColor(user: UserProfile): string {
    const code = (user.userName || user.email || 'u').charCodeAt(0);
    return this.palette[code % this.palette.length];
  }
}
