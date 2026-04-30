using BookingService.Application.Interfaces.Services;
using BookingService.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookingService.Infrastructure.Services
{
    public class InvoiceService : IInvoiceService
    {
        public Task<byte[]> GenerateInvoiceAsync(Booking booking, string userName, string hotelName)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("StayEasy").FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Your home away from home").FontSize(10).Italic().FontColor(Colors.Grey.Medium);
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().AlignRight().Text("INVOICE").FontSize(20).SemiBold();
                            col.Item().AlignRight().Text($"Invoice #: {booking.Id.ToString().Substring(0, 8).ToUpper()}");
                            col.Item().AlignRight().Text($"Date: {DateTime.Now:dd MMM yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // Info Section
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Billed To:").SemiBold();
                                c.Item().Text(userName);
                                c.Item().Text("Guest");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignRight().Text("Property Details:").SemiBold();
                                c.Item().AlignRight().Text(hotelName);
                                c.Item().AlignRight().Text("Confirmed Stay");
                            });
                        });

                        col.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("Description");
                                header.Cell().Element(CellStyle).AlignRight().Text("Rate");
                                header.Cell().Element(CellStyle).AlignRight().Text("Nights");
                                header.Cell().Element(CellStyle).AlignRight().Text("Total");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            int index = 1;
                            foreach (var item in booking.BookingItems)
                            {
                                table.Cell().Element(CellStyle).Text(index++.ToString());
                                table.Cell().Element(CellStyle).Text($"{item.CheckInDate:dd MMM} - {item.CheckOutDate:dd MMM} (Room ID: {item.RoomTypeId.ToString().Substring(0, 5)})");
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{item.PricePerNight:N2}");
                                table.Cell().Element(CellStyle).AlignRight().Text(item.Nights.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{item.Subtotal:N2}");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                }
                            }
                        });

                        // Summary Section
                        col.Item().AlignRight().PaddingTop(10).Column(c =>
                        {
                            var subtotal = booking.BookingItems.Sum(x => x.Subtotal);
                            var taxes = Math.Round(subtotal * 0.09m, 2);
                            var discount = subtotal + taxes - booking.TotalAmount;

                            c.Item().Text($"Subtotal: ₹{subtotal:N2}");
                            c.Item().Text($"Taxes (9%): ₹{taxes:N2}");
                            if (discount > 0)
                            {
                                c.Item().Text($"Discount: -₹{discount:N2}").FontColor(Colors.Red.Medium);
                            }
                            c.Item().PaddingTop(5).Text($"Grand Total: ₹{booking.TotalAmount:N2}").FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                        });

                        col.Item().PaddingTop(30).Column(c =>
                        {
                            c.Item().Text("Important Information:").SemiBold();
                            c.Item().Text("• Please carry a valid ID proof at the time of check-in.");
                            c.Item().Text("• Standard check-in time is 12:00 PM and check-out time is 11:00 AM.");
                            c.Item().Text("• For cancellations, please refer to our refund policy.");
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return Task.FromResult(document.GeneratePdf());
        }

        public Task<byte[]> GenerateAdminReportAsync(List<Booking> bookings)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("StayEasy Business Report").FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text($"Generated on: {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // Executive Summary
                        col.Item().BorderBottom(1).PaddingBottom(5).Text("Executive Summary").FontSize(16).SemiBold();
                        
                        var totalRevenue = bookings.Where(b => b.Status == BookingStatus.Confirmed).Sum(b => b.TotalAmount);
                        var totalBookings = bookings.Count;
                        var confirmedCount = bookings.Count(b => b.Status == BookingStatus.Confirmed);
                        var pendingCount = bookings.Count(b => b.Status == BookingStatus.Pending);
                        var cancelledCount = bookings.Count(b => b.Status == BookingStatus.Cancelled);

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Total Revenue").FontSize(12).SemiBold();
                                c.Item().AlignCenter().Text($"₹{totalRevenue:N2}").FontSize(18).SemiBold().FontColor(Colors.Green.Medium);
                            });

                            row.ConstantItem(10);

                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Total Bookings").FontSize(12).SemiBold();
                                c.Item().AlignCenter().Text(totalBookings.ToString()).FontSize(18).SemiBold().FontColor(Colors.Blue.Medium);
                            });
                        });

                        col.Item().PaddingTop(20).Text("Booking Status Breakdown").FontSize(14).SemiBold();
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).PaddingVertical(5).Text("Confirmed");
                                header.Cell().BorderBottom(1).PaddingVertical(5).Text("Pending");
                                header.Cell().BorderBottom(1).PaddingVertical(5).Text("Cancelled");
                            });

                            table.Cell().PaddingVertical(5).Text(confirmedCount.ToString());
                            table.Cell().PaddingVertical(5).Text(pendingCount.ToString());
                            table.Cell().PaddingVertical(5).Text(cancelledCount.ToString());
                        });

                        col.Item().PaddingTop(30).BorderBottom(1).PaddingBottom(5).Text("Recent Detailed Bookings").FontSize(16).SemiBold();
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("Booking ID");
                                header.Cell().Element(CellStyle).Text("Date");
                                header.Cell().Element(CellStyle).AlignRight().Text("Amount");
                                header.Cell().Element(CellStyle).AlignRight().Text("Status");

                                static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1);
                            });

                            int index = 1;
                            foreach (var b in bookings.OrderByDescending(x => x.BookingDate).Take(20))
                            {
                                table.Cell().Element(CellStyle).Text(index++.ToString());
                                table.Cell().Element(CellStyle).Text(b.Id.ToString().Substring(0, 8).ToUpper());
                                table.Cell().Element(CellStyle).Text(b.BookingDate.ToString("dd MMM yyyy"));
                                table.Cell().Element(CellStyle).AlignRight().Text($"₹{b.TotalAmount:N2}");
                                table.Cell().Element(CellStyle).AlignRight().Text(b.Status.ToString());

                                static IContainer CellStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("StayEasy Admin Confidential - Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return Task.FromResult(document.GeneratePdf());
        }
    }
}
