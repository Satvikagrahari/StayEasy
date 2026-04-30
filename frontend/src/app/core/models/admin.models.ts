export interface DashboardSummary {
  totalBookings: number;
  pendingBookings: number;
  confirmedBookings: number;
  cancelledBookings: number;
  totalRevenue: number;
  todayBookings: number;
}

export interface BookingTrend {
  date: string;
  count: number;
}

export interface RevenueTrend {
  month: string;
  revenue: number;
}

export interface BookingStatusDistribution {
  status: string;
  count: number;
}

export interface OccupancyReport {
  date: string;           // "yyyy-MM-dd"
  occupiedBookings: number;
}

export interface RevenueReport {
  date: string;
  revenue: number;
  bookingCount: number;
}

export interface CancellationReport {
  date: string;
  cancellations: number;
}
