using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SportClub.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHasDataSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ClientProfiles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "Id", "CreatedAt", "Description", "DurationUnit", "DurationValue", "IsArchived", "MaxVisits", "Name", "PlanType", "Price" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Необмежені відвідування протягом 30 днів", "Months", 1, false, null, "Місячний безлімітний", "Unlimited", 1200m },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "8 відвідувань, дійсний 60 днів", "Days", 60, false, 8, "8 занять", "LimitedVisits", 800m },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "12 відвідувань, дійсний 90 днів", "Days", 90, false, 12, "12 занять", "LimitedVisits", 1100m },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Необмежені відвідування протягом 3 місяців", "Months", 3, false, null, "Квартальний безлімітний", "Unlimited", 3000m }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@sportclub.ua", "Адміністратор", "$2a$11$lZaR85nu83EwV9eVmQF5AeEpUDLTwcSnynft5HyrQRU22qR59EIeS", "Admin" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "reception@sportclub.ua", "Рецепціоніст", "$2a$11$TmM/AQzkz1q6hkHbuxomf.miTS9a/1yJRjIoMSnc2l4zTobkAXg6W", "Receptionist" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "client@sportclub.ua", "Іван Петренко", "$2a$11$JkZzKqbH7PUqNclagk3aXOBqQkhIPCxkbgYIeET7B6h.9cjrCBsym", "Client" }
                });

            migrationBuilder.InsertData(
                table: "ClientProfiles",
                columns: new[] { "Id", "Barcode", "CreatedAt", "DateOfBirth", "IsBlocked", "Notes", "Phone", "UserId" },
                values: new object[] { 1, "SC-000001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1990, 5, 15, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "+380501234567", 3 });
        }
    }
}
