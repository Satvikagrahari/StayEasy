import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../core/services/admin-api.service';
import { OccupancyReport, RevenueReport, CancellationReport } from '../../core/models/admin.models';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { forkJoin } from 'rxjs';

type ReportTab = 'occupancy' | 'revenue' | 'cancellations';
Chart.register(...registerables);
Chart.defaults.font.family = 'Inter, sans-serif';
Chart.defaults.font.size = 12;
Chart.defaults.color = '#6B7280';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.css'
})
export class ReportsComponent implements AfterViewInit, OnDestroy {
  private adminApi = inject(AdminApiService);
  private canvasRef?: ElementRef<HTMLCanvasElement>;
  @ViewChild('reportCanvas')
  set reportCanvas(ref: ElementRef<HTMLCanvasElement> | undefined) {
    this.canvasRef = ref;
    this.scheduleChartRender();
  }

  activeTab = signal<ReportTab>('revenue');
  isLoading = signal(false);
  error = signal<string | null>(null);

  startDate = '';
  endDate = '';
  rangePreset = 'Last 7 days';
  private viewReady = false;
  private chartRenderId: number | null = null;
  private chart?: Chart;

  occupancy = signal<OccupancyReport[]>([]);
  revenue = signal<RevenueReport[]>([]);
  cancellations = signal<CancellationReport[]>([]);
  private rawOccupancy: OccupancyReport[] = [];
  private rawRevenue: RevenueReport[] = [];
  private rawCancellations: CancellationReport[] = [];

  totalRevenue = () => this.revenue().reduce((s, r) => s + this.toNumber(r.revenue), 0);
  totalBookings = () => this.revenue().reduce((s, r) => s + this.toNumber(r.bookingCount), 0);
  avgBookingValue = () => this.totalBookings() ? this.totalRevenue() / this.totalBookings() : 0;
  avgOccupancy = () => {
    const activeDays = this.occupancy().filter(row => this.toNumber(row.occupiedBookings) > 0);
    const denominator = activeDays.length || this.occupancy().length;
    if (!denominator) return 0;
    const totalOccupied = this.occupancy().reduce((s, r) => s + this.toNumber(r.occupiedBookings), 0);
    return Math.round((totalOccupied / denominator) * 10) / 10;
  };
  totalCancellations = () => this.cancellations().reduce((s, r) => s + this.toNumber(r.cancellations), 0);
  cancellationRate = () => {
    const totalDecisions = this.totalBookings() + this.totalCancellations();
    return totalDecisions ? Math.round((this.totalCancellations() / totalDecisions) * 100) : 0;
  };

  constructor() {
    this.applyPreset('Last 7 days', false);
    queueMicrotask(() => this.load());
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.scheduleChartRender();
  }

  ngOnDestroy(): void {
    if (this.chartRenderId !== null) {
      cancelAnimationFrame(this.chartRenderId);
    }
    this.chart?.destroy();
  }

  setTab(tab: ReportTab): void {
    this.activeTab.set(tab);
    this.applyLocalFilters();
  }

  applyPreset(preset: string, shouldLoad = true): void {
    this.rangePreset = preset;
    this.error.set(null);
    if (preset !== 'Custom') {
      const today = this.startOfLocalDay(new Date());
      const start = new Date(today);
      if (preset === 'Last 30 days') start.setDate(today.getDate() - 29);
      else if (preset === 'This month') start.setDate(1);
      else start.setDate(today.getDate() - 6);
      this.startDate = this.toLocalDateInput(start);
      this.endDate = this.toLocalDateInput(today);
      if (shouldLoad) this.applyLocalFilters();
    }
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    const fallbackStart = this.toLocalDateInput(new Date(new Date().getFullYear() - 1, new Date().getMonth(), new Date().getDate()));
    const fallbackEnd = this.toLocalDateInput(this.startOfLocalDay(new Date()));
    const reportStart = this.startDate && this.startDate < fallbackStart ? this.startDate : fallbackStart;
    const reportEnd = this.endDate && this.endDate > fallbackEnd ? this.endDate : fallbackEnd;
    forkJoin({
      occupancy: this.adminApi.getOccupancyReport(reportStart, reportEnd),
      revenue: this.adminApi.getRevenueReport(reportStart, reportEnd),
      cancellations: this.adminApi.getCancellationReport(reportStart, reportEnd)
    }).subscribe({
      next: ({ occupancy, revenue, cancellations }) => {
        this.rawOccupancy = occupancy ?? [];
        this.rawRevenue = revenue ?? [];
        this.rawCancellations = cancellations ?? [];
        this.applyLocalFilters(false);
        this.isLoading.set(false);
        this.scheduleChartRender();
      },
      error: () => {
        this.error.set('Failed to load report.');
        this.isLoading.set(false);
        this.chart?.destroy();
      }
    });
  }

  applyCustomRange(): void {
    if (!this.startDate || !this.endDate) {
      this.error.set('Select both start and end dates.');
      return;
    }
    if (this.startDate > this.endDate) {
      this.error.set('Start date must be before end date.');
      return;
    }
    this.error.set(null);
    this.rangePreset = 'Custom';
    this.applyLocalFilters();
  }

  dateRangeLabel(): string {
    return `${this.formatDisplayDate(this.startDate)} - ${this.formatDisplayDate(this.endDate)}`;
  }

  hasActiveData(): boolean {
    if (this.activeTab() === 'revenue') return this.revenue().some(row => row.revenue || row.bookingCount);
    if (this.activeTab() === 'occupancy') return this.occupancy().some(row => row.occupiedBookings);
    return this.cancellations().some(row => row.cancellations);
  }

  private renderChart(): void {
    if (!this.viewReady || !this.canvasRef || this.isLoading()) return;
    this.chart?.destroy();
    const tab = this.activeTab();
    if (tab === 'revenue') {
      const data = this.revenue();
      this.chart = new Chart(this.canvasRef.nativeElement, {
        type: 'line',
        data: { labels: data.map(x => this.formatDisplayDate(x.date)), datasets: [{ label: 'Revenue', data: data.map(x => this.toNumber(x.revenue)), borderColor: '#2563EB', backgroundColor: 'rgba(37,99,235,.1)', fill: true, tension: .4, pointRadius: 3, pointHoverRadius: 5 }] },
        options: this.options('Revenue over time')
      });
    } else if (tab === 'occupancy') {
      const data = this.occupancy();
      this.chart = new Chart(this.canvasRef.nativeElement, {
        type: 'bar',
        data: { labels: data.map(x => this.formatDisplayDate(x.date)), datasets: [{ label: 'Rooms occupied', data: data.map(x => this.toNumber(x.occupiedBookings)), backgroundColor: '#0EA5E9', borderRadius: 8 }] },
        options: this.options('Rooms occupied per day')
      });
    } else {
      const data = this.cancellations();
      this.chart = new Chart(this.canvasRef.nativeElement, {
        type: 'bar',
        data: { labels: data.map(x => this.formatDisplayDate(x.date)), datasets: [{ label: 'Cancellations', data: data.map(x => this.toNumber(x.cancellations)), backgroundColor: 'rgba(239,68,68,.8)', borderRadius: 8 }] },
        options: this.options('Cancellations over time')
      });
    }
  }

  private options(title: string): ChartConfiguration['options'] {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        title: { display: true, text: title, align: 'start', font: { size: 16, weight: 'bold' } },
        tooltip: {
          backgroundColor: '#0F172A',
          padding: 12,
          titleColor: '#FFFFFF',
          bodyColor: '#E5E7EB',
          callbacks: {
            label: (context) => {
              const value = Number(context.parsed.y ?? 0);
              if (this.activeTab() === 'revenue') return `Revenue: ${value.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}`;
              return `${context.dataset.label}: ${value.toLocaleString('en-IN')}`;
            }
          }
        }
      },
      scales: {
        x: { grid: { color: '#E5E7EB', lineWidth: .5 }, ticks: { maxRotation: 0, autoSkip: true, maxTicksLimit: 8 } },
        y: {
          beginAtZero: true,
          grid: { color: '#E5E7EB', lineWidth: .5 },
          ticks: {
            callback: (value) => this.activeTab() === 'revenue'
              ? `₹${Number(value).toLocaleString('en-IN')}`
              : Number(value).toLocaleString('en-IN')
          }
        }
      }
    };
  }

  private applyLocalFilters(shouldRender = true): void {
    this.error.set(null);
    const days = this.dateRangeDays();
    const start = this.startDate;
    const end = this.endDate;

    const revenueByDate = this.groupByDate(this.rawRevenue, start, end, () => ({ revenue: 0, bookingCount: 0 }), (target, row) => {
      target.revenue += this.toNumber(row.revenue);
      target.bookingCount += this.toNumber(row.bookingCount);
    });
    const occupancyByDate = this.groupByDate(this.rawOccupancy, start, end, () => ({ occupiedBookings: 0 }), (target, row) => {
      target.occupiedBookings += this.toNumber(row.occupiedBookings);
    });
    const cancellationsByDate = this.groupByDate(this.rawCancellations, start, end, () => ({ cancellations: 0 }), (target, row) => {
      target.cancellations += this.toNumber(row.cancellations);
    });

    this.revenue.set(days.map(date => ({ date, ...(revenueByDate.get(date) ?? { revenue: 0, bookingCount: 0 }) } as RevenueReport)));
    this.occupancy.set(days.map(date => ({ date, ...(occupancyByDate.get(date) ?? { occupiedBookings: 0 }) } as OccupancyReport)));
    this.cancellations.set(days.map(date => ({ date, ...(cancellationsByDate.get(date) ?? { cancellations: 0 }) } as CancellationReport)));
    if (shouldRender) this.scheduleChartRender();
  }

  private groupByDate<T, V extends Record<string, number>>(
    rows: T[],
    start: string,
    end: string,
    makeEmpty: () => V,
    merge: (target: V, row: T) => void
  ): Map<string, V> {
    const map = new Map<string, V>();
    for (const row of rows) {
      const date = this.normalizeDate((row as { date?: string }).date);
      if (!date || date < start || date > end) continue;
      const current = map.get(date) ?? makeEmpty();
      merge(current, row);
      map.set(date, current);
    }
    return map;
  }

  private dateRangeDays(): string[] {
    const days: string[] = [];
    const start = this.parseDateInput(this.startDate);
    const end = this.parseDateInput(this.endDate);
    if (!start || !end || start > end) return days;
    for (const day = new Date(start); day <= end; day.setDate(day.getDate() + 1)) {
      days.push(this.toLocalDateInput(day));
    }
    return days;
  }

  private scheduleChartRender(): void {
    if (this.chartRenderId !== null) {
      cancelAnimationFrame(this.chartRenderId);
    }
    this.chartRenderId = requestAnimationFrame(() => {
      this.chartRenderId = null;
      this.renderChart();
    });
  }

  private normalizeDate(value?: string): string | null {
    if (!value) return null;
    const match = value.match(/\d{4}-\d{2}-\d{2}/);
    if (match) return match[0];
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : this.toLocalDateInput(parsed);
  }

  private parseDateInput(value: string): Date | null {
    if (!value) return null;
    const [year, month, day] = value.split('-').map(Number);
    if (!year || !month || !day) return null;
    return new Date(year, month - 1, day);
  }

  private startOfLocalDay(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
  }

  private toLocalDateInput(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private formatDisplayDate(value: string): string {
    const date = this.parseDateInput(value);
    if (!date) return value;
    return date.toLocaleDateString('en-IN', { day: 'numeric', month: 'short' });
  }

  private toNumber(value: unknown): number {
    const numeric = Number(value ?? 0);
    return Number.isFinite(numeric) ? numeric : 0;
  }
}
