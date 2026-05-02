import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs/operators';
import { AuthService } from '../../core/services/auth.service';
import { SignupRequest } from '../../core/models/auth.models';

export function passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
  return group.get('password')?.value === group.get('confirmPassword')?.value ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  registerForm = this.fb.group(
    {
      email: ['', [Validators.required, Validators.email]],
      userName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(80)]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{10,15}$/)]],
      password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[A-Z])(?=.*\d).+$/)]],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordMatchValidator }
  );

  errorMessage: string | null = null;
  isLoading = false;
  showPassword = false;
  showConfirmPassword = false;
  termsAccepted = false;

  get passwordMismatch(): boolean {
    return this.registerForm.hasError('passwordMismatch') &&
      (this.registerForm.get('confirmPassword')?.touched ?? false);
  }

  get passwordValue(): string {
    return this.registerForm.get('password')?.value ?? '';
  }

  get hasMinLength(): boolean {
    return this.passwordValue.length >= 8;
  }

  get hasUpperCase(): boolean {
    return /[A-Z]/.test(this.passwordValue);
  }

  get hasDigit(): boolean {
    return /\d/.test(this.passwordValue);
  }

  passwordStrength(): { score: number; label: string } {
    const value = this.registerForm.get('password')?.value ?? '';
    if (!value) return { score: 0, label: 'Weak' };
    if (value.length >= 10 && /[^A-Za-z0-9]/.test(value)) return { score: 4, label: 'Strong' };
    if (value.length >= 8 && /\d/.test(value)) return { score: 3, label: 'Good' };
    if (value.length >= 6) return { score: 2, label: 'Fair' };
    return { score: 1, label: 'Weak' };
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    if (!this.termsAccepted) {
      this.errorMessage = 'Please accept the terms to continue.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;

    const { email, userName, password, phoneNumber } = this.registerForm.getRawValue();
    const signupRequest: SignupRequest = {
      userName: userName!,
      email: email!,
      password: password!,
      phoneNumber: phoneNumber!
    };

    this.authService.signup(signupRequest).pipe(
      switchMap(() => this.authService.sendOtp({ email: email! }))
    ).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/verify-otp'], { queryParams: { email } });
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message ?? 'Registration failed. Please try again.';
      }
    });
  }
}
