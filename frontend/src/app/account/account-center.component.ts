import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../core/services/auth.service';
import { ToastService } from '../core/services/toast.service';
import { UpdateProfileRequest, UserProfile } from '../core/models/auth.models';

@Component({
  selector: 'app-account-center',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './account-center.component.html',
  styleUrl: './account-center.component.css'
})
export class AccountCenterComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private toast = inject(ToastService);

  profile = signal<UserProfile | null>(null);
  isLoading = signal(true);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);

  profileForm = this.fb.group({
    userName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(80)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{10,15}$/)]]
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.getProfile().subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.profileForm.patchValue({
          userName: profile.userName,
          email: profile.email,
          phoneNumber: profile.phoneNumber
        });
        this.profileForm.markAsPristine();
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message ?? 'Unable to load your account details right now.');
        this.isLoading.set(false);
      }
    });
  }

  save(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    const request = this.profileForm.getRawValue() as UpdateProfileRequest;
    this.isSaving.set(true);

    this.authService.updateProfile(request).subscribe({
      next: () => {
        this.toast.success('Account details updated.');
        this.isSaving.set(false);
        this.loadProfile();
      },
      error: (err) => {
        this.toast.error(err.error?.message ?? 'Failed to update account details.');
        this.isSaving.set(false);
      }
    });
  }
}
