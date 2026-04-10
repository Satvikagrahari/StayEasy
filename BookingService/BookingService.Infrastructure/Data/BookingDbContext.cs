using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Infrastructure.Data
{
    public class BookingDbContext : DbContext
    {
        public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
        {
        }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<BookingItem> BookingItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Booking>()
                .Property(h => h.TotalAmount)
                .HasPrecision(18, 2); 
  
            modelBuilder.Entity<CartItem>()
                .Property(h => h.PriceSnapshot)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Booking>()
            .HasMany(b => b.BookingItems)
            .WithOne(bi => bi.Booking)
            .HasForeignKey(bi => bi.BookingId);
        }
    }

}