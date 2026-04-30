import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { LoginRequest } from '../../core/models/auth.models';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private toast = inject(ToastService);

  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  errorMessage: string | null = null;
  isLoading = false;
  showPassword = false;
  private returnUrl = '/hotels';
  showVerifiedBanner = false;

  ngOnInit(): void {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] ?? '/hotels';
    this.showVerifiedBanner = this.route.snapshot.queryParams['verified'] === 'true';
  }

  onSubmit() {
    if (this.loginForm.valid) {
      this.isLoading = true;
      this.errorMessage = null;
      
      this.authService.login(this.loginForm.value as LoginRequest).subscribe({
        next: (response) => {
          this.isLoading = false;
          const user = this.authService.getCurrentUser();
          if (user?.role?.toLowerCase() === 'admin') {
            this.router.navigate(['/admin/dashboard']);
          } else {
            this.router.navigateByUrl(this.returnUrl);
          }
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = err.error?.message ?? 'Invalid login credentials. Please try again.';
          this.toast.error(this.errorMessage ?? 'Login failed.');
        }
      });
    } else {
      this.loginForm.markAllAsTouched();
    }
  }
}
