using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatedhotelentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "RoomTypes",
                newName: "Type");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Hotels",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Hotels",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StarRating",
                table: "Hotels",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "StarRating",
                table: "Hotels");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "RoomTypes",
                newName: "Name");
        }
    }
}
