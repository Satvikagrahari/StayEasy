import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './verify-otp.component.html',
  styleUrl: './verify-otp.component.css'
})
export class VerifyOtpComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  otpForm = this.fb.group({
    code: ['', Validators.required]
  });

  email = '';
  errorMessage: string | null = null;
  isLoading = false;
  isSending = false;

  ngOnInit(): void {
    this.email = this.route.snapshot.queryParams['email'] ?? '';
  }

  resend(): void {
    if (!this.email) return;
    this.isSending = true;
    this.authService.sendOtp({ email: this.email }).subscribe({
      next: () => { this.isSending = false; },
      error: () => { this.isSending = false; }
    });
  }

  onSubmit(): void {
    if (this.otpForm.invalid || !this.email) {
      this.otpForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;

    this.authService.verifyOtp({
      email: this.email,
      code: this.otpForm.value.code!
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/login'], { queryParams: { verified: 'true' } });
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message ?? 'OTP verification failed. Please try again.';
      }
    });
  }
}
