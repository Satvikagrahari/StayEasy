import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { AdminApiService } from '../../core/services/admin-api.service';
import { BookingStatusDistribution, BookingTrend, DashboardSummary, RevenueTrend } from '../../core/models/admin.models';
import { Booking } from '../../core/models/booking.models';

Chart.register(...registerables);

import { BookingApiService } from '../../core/services/booking-api.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  private adminApi = inject(AdminApiService);
  private bookingApi = inject(BookingApiService);

  @ViewChild('bookingTrendCanvas') bookingTrendCanvas?: ElementRef<HTMLCanvasElement>;
  @ViewChild('revenueTrendCanvas') revenueTrendCanvas?: ElementRef<HTMLCanvasElement>;
  @ViewChild('statusCanvas') statusCanvas?: ElementRef<HTMLCanvasElement>;

  summary = signal<DashboardSummary | null>(null);
  animatedSummary = signal<DashboardSummary | null>(null);
  recentBookings = signal<Booking[]>([]);
  bookingTrends = signal<BookingTrend[]>([]);
  revenueTrends = signal<RevenueTrend[]>([]);
  statusDistribution = signal<BookingStatusDistribution[]>([]);
  isLoading = signal(true);
  error = signal<string | null>(null);

  private viewReady = false;
  private bookingTrendChart?: Chart;
  private revenueTrendChart?: Chart;
  private statusChart?: Chart;

  ngOnInit(): void {
    this.loadDashboard();
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.renderCharts();
  }

  ngOnDestroy(): void {
    this.destroyCharts();
  }

  statusBadge(status: string): string {
    const map: Record<string, string> = {
      Pending: 'badge badge-warning',
      Confirmed: 'badge badge-success',
      Cancelled: 'badge badge-danger',
      Failed: 'badge badge-danger',
      RefundRequested: 'badge badge-info'
    };
    return map[status] ?? 'badge badge-secondary';
  }

  copyId(id: string): void {
    navigator.clipboard?.writeText(id);
  }

  sparkline(points: number[]): string {
    const max = Math.max(...points, 1);
    return points.map((point, index) => `${index * 16},${55 - (point / max) * 48}`).join(' ');
  }

  inr(value: number): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(value || 0);
  }

  private animateSummary(target: DashboardSummary): void {
    const start = performance.now();
    const duration = 800;
    const tick = (now: number) => {
      const progress = Math.min(1, (now - start) / duration);
      const ease = 1 - Math.pow(1 - progress, 3);
      this.animatedSummary.set({
        totalBookings: Math.round(target.totalBookings * ease),
        pendingBookings: Math.round(target.pendingBookings * ease),
        confirmedBookings: Math.round(target.confirmedBookings * ease),
        cancelledBookings: Math.round(target.cancelledBookings * ease),
        totalRevenue: Math.round(target.totalRevenue * ease),
        todayBookings: Math.round(target.todayBookings * ease)
      });
      if (progress < 1) requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  }

  private loadDashboard(): void {
    this.isLoading.set(true);
    this.error.set(null);

    forkJoin({
      summary: this.adminApi.getDashboardStats(),
      recentBookings: this.adminApi.getRecentBookings(5),
      bookingTrends: this.adminApi.getBookingTrends(7),
      revenueTrends: this.adminApi.getRevenueTrends(12),
      statusDistribution: this.adminApi.getBookingStatusDistribution()
    }).subscribe({
      next: data => {
        this.summary.set(data.summary);
        this.animateSummary(data.summary);
        this.recentBookings.set(data.recentBookings);
        this.bookingTrends.set(data.bookingTrends);
        this.revenueTrends.set(data.revenueTrends);
        this.statusDistribution.set(data.statusDistribution);
        this.isLoading.set(false);
        queueMicrotask(() => this.renderCharts());
      },
      error: () => {
        this.error.set('Failed to load dashboard data.');
        this.isLoading.set(false);
      }
    });
  }

  private renderCharts(): void {
    if (!this.viewReady || this.isLoading()) return;

    this.destroyCharts();
    this.bookingTrendChart = this.createBookingTrendChart();
    this.revenueTrendChart = this.createRevenueTrendChart();
    this.statusChart = this.createStatusChart();
  }

  private createBookingTrendChart(): Chart | undefined {
    const canvas = this.bookingTrendCanvas?.nativeElement;
    if (!canvas) return undefined;

    const trends = this.bookingTrends();
    return new Chart(canvas, {
      type: 'line',
      data: {
        labels: trends.map(item => item.date),
        datasets: [{
          label: 'Bookings',
          data: trends.map(item => item.count),
          borderColor: '#0d6efd',
          backgroundColor: 'rgba(13, 110, 253, .12)',
          fill: true,
          tension: .35,
          pointRadius: 4
        }]
      },
      options: this.chartOptions('Booking Trends')
    });
  }

  private createRevenueTrendChart(): Chart | undefined {
    const canvas = this.revenueTrendCanvas?.nativeElement;
    if (!canvas) return undefined;

    const trends = this.revenueTrends();
    return new Chart(canvas, {
      type: 'bar',
      data: {
        labels: trends.map(item => item.month),
        datasets: [{
          label: 'Revenue',
          data: trends.map(item => item.revenue),
          backgroundColor: '#198754',
          borderRadius: 6
        }]
      },
      options: this.chartOptions('Monthly Revenue')
    });
  }

  private createStatusChart(): Chart | undefined {
    const canvas = this.statusCanvas?.nativeElement;
    if (!canvas) return undefined;

    const distribution = this.statusDistribution();
    return new Chart(canvas, {
      type: 'pie',
      data: {
        labels: distribution.map(item => item.status),
        datasets: [{
          data: distribution.map(item => item.count),
          backgroundColor: ['#ffc107', '#198754', '#dc3545', '#6c757d'],
          borderColor: '#ffffff',
          borderWidth: 2
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'bottom' },
          title: { display: true, text: 'Booking Status Distribution' }
        }
      }
    });
  }

  private chartOptions(title: string): ChartConfiguration['options'] {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        title: { display: true, text: title }
      },
      scales: {
        y: {
          beginAtZero: true,
          ticks: { precision: 0 }
        }
      }
    };
  }

  private destroyCharts(): void {
    this.bookingTrendChart?.destroy();
    this.revenueTrendChart?.destroy();
    this.statusChart?.destroy();
    this.bookingTrendChart = undefined;
    this.revenueTrendChart = undefined;
    this.statusChart = undefined;
  }

  downloadReport(): void {
    this.bookingApi.downloadAdminReport().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `StayEasy_Business_Report_${new Date().toISOString().split('T')[0]}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => this.error.set('Failed to download business report.')
    });
  }
}
