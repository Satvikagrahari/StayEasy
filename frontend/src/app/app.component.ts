import { Component, inject, signal } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { animate, style, transition, trigger } from '@angular/animations';
import { NavbarComponent } from './shared/navbar/navbar.component';
import { FooterComponent } from './shared/footer/footer.component';
import { ToastComponent } from './shared/toast/toast.component';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, FooterComponent, ToastComponent, AsyncPipe],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
  animations: [
    trigger('pageTransition', [
      transition('* => *', [
        style({ opacity: 0, transform: 'translateY(10px)' }),
        animate('200ms ease', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ])
  ]
})
export class AppComponent {
  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private authService = inject(AuthService);

  title = 'frontend';
  isAdminRoute = signal(false);
  isAuthRoute = signal(false);
  showPromo = signal(localStorage.getItem('stayEasyPromoDismissed') !== 'true');
  adminSidebarOpen = signal(false);
  pageTitle = signal('Dashboard');
  currentUser$ = this.authService.currentUser$;

  constructor() {
    this.syncShellState();
    this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => this.syncShellState());
  }

  private syncShellState(): void {
    this.isAdminRoute.set(this.router.url.startsWith('/admin'));
    this.isAuthRoute.set(['/login', '/register', '/verify-otp', '/forgot-password', '/auth'].some(path => this.router.url.startsWith(path)));
    this.adminSidebarOpen.set(false);
    this.pageTitle.set(this.resolveRouteTitle() ?? 'Dashboard');
  }

  dismissPromo(): void {
    localStorage.setItem('stayEasyPromoDismissed', 'true');
    this.showPromo.set(false);
  }

  private resolveRouteTitle(): string | null {
    let route = this.activatedRoute.firstChild;
    while (route?.firstChild) {
      route = route.firstChild;
    }

    return route?.snapshot.data['title'] ?? null;
  }

  routeKey(outlet: RouterOutlet): string {
    return outlet.activatedRouteData?.['title'] ?? this.router.url;
  }
}
