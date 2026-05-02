import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

type ResetStep = 'email' | 'otp' | 'password' | 'done';

function passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
  return group.get('newPassword')?.value === group.get('confirmPassword')?.value ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  step: ResetStep = 'email';
  email = '';
  verifiedCode = '';
  errorMessage: string | null = null;
  successMessage: string | null = null;
  isLoading = false;
  isResending = false;
  showPassword = false;
  showConfirmPassword = false;

  emailForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  otpForm = this.fb.group({
    code: ['', [Validators.required, Validators.pattern(/^[0-9]{6}$/)]]
  });

  passwordForm = this.fb.group(
    {
      newPassword: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[A-Z])(?=.*\d).+$/)]],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordMatchValidator }
  );

  get passwordMismatch(): boolean {
    return this.passwordForm.hasError('passwordMismatch') &&
      (this.passwordForm.get('confirmPassword')?.touched ?? false);
  }

  sendOtp(): void {
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;
    this.successMessage = null;
    this.email = this.emailForm.value.email!.trim().toLowerCase();

    this.authService.sendPasswordResetOtp({ email: this.email }).subscribe({
      next: () => {
        this.isLoading = false;
        this.step = 'otp';
        this.successMessage = 'OTP sent to your email.';
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message ?? 'Could not send OTP. Please try again.';
      }
    });
  }

  resendOtp(): void {
    if (!this.email) return;

    this.isResending = true;
    this.errorMessage = null;

    this.authService.sendPasswordResetOtp({ email: this.email }).subscribe({
      next: () => {
        this.isResending = false;
        this.successMessage = 'A new OTP has been sent.';
      },
      error: (err) => {
        this.isResending = false;
        this.errorMessage = err.error?.message ?? 'Could not resend OTP. Please try again.';
      }
    });
  }

  verifyOtp(): void {
    if (this.otpForm.invalid || !this.email) {
      this.otpForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;
    this.successMessage = null;
    this.verifiedCode = this.otpForm.value.code!.trim();

    this.authService.verifyPasswordResetOtp({
      email: this.email,
      code: this.verifiedCode
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.step = 'password';
        this.successMessage = 'OTP verified. Create your new password.';
      },
      error: (err) => {
        this.isLoading = false;
        this.verifiedCode = '';
        this.errorMessage = err.error?.message ?? 'OTP verification failed. Please try again.';
      }
    });
  }

  resetPassword(): void {
    if (this.passwordForm.invalid || !this.email || !this.verifiedCode) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;
    this.successMessage = null;

    this.authService.resetPassword({
      email: this.email,
      code: this.verifiedCode,
      newPassword: this.passwordForm.value.newPassword!
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.step = 'done';
        this.successMessage = 'Password updated successfully.';
        this.toast.success('Password updated successfully.');
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message ?? 'Could not update password. Please try again.';
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
