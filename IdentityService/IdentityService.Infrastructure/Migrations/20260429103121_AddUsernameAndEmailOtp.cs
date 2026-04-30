using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernameAndEmailOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailOtpCode",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailOtpExpiryTime",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Users",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                WITH NumberedUsers AS
                (
                    SELECT
                        Id,
                        COALESCE(NULLIF(LEFT(TransformedEmailNames.BaseUserName, 60), ''), 'user') AS BaseUserName,
                        ROW_NUMBER() OVER (
                            PARTITION BY COALESCE(NULLIF(LEFT(TransformedEmailNames.BaseUserName, 60), ''), 'user')
                            ORDER BY Id
                        ) AS DuplicateNumber
                    FROM Users
                    CROSS APPLY
                    (
                        SELECT REPLACE(REPLACE(REPLACE(LOWER(LEFT(Email, CHARINDEX('@', Email + '@') - 1)), '.', '_'), '-', '_'), '+', '_') AS BaseUserName
                    ) AS TransformedEmailNames
                    WHERE UserName = ''
                )
                UPDATE Users
                SET UserName = CASE
                    WHEN NumberedUsers.DuplicateNumber = 1 THEN NumberedUsers.BaseUserName
                    ELSE CONCAT(NumberedUsers.BaseUserName, '_', NumberedUsers.DuplicateNumber)
                END
                FROM Users
                INNER JOIN NumberedUsers ON Users.Id = NumberedUsers.Id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_UserName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailOtpCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailOtpExpiryTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Users");
        }
    }
}
