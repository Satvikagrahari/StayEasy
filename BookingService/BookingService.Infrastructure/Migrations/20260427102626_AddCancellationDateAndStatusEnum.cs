using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationDateAndStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Bookings");

            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationDate",
                table: "Bookings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationDate",
                table: "Bookings");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
