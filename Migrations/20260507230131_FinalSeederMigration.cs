using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportClub.Migrations
{
    /// <inheritdoc />
    public partial class FinalSeederMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$lZaR85nu83EwV9eVmQF5AeEpUDLTwcSnynft5HyrQRU22qR59EIeS");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$TmM/AQzkz1q6hkHbuxomf.miTS9a/1yJRjIoMSnc2l4zTobkAXg6W");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "PasswordHash",
                value: "$2a$11$JkZzKqbH7PUqNclagk3aXOBqQkhIPCxkbgYIeET7B6h.9cjrCBsym");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$JvMDtG7X3iAn6KCYjcCpQ.pHxKkvhhzJPBGVrJxmF1t90hzQXCZWi");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$lLPS1axD3x9QnMI1icSYneQ7k6I4FYdIFm9mEuTx9E3r2TZ.FtAge");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "PasswordHash",
                value: "$2a$11$s1icZlMUo1z6qqpUruZ.2.X4BAKuMkCQWuW3WhUxfwi39zDQ86NLS");
        }
    }
}
