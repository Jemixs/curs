using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportClub.Migrations
{
    /// <inheritdoc />
    public partial class MakePasswordNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$Fr35k/5EKp6v0FJKgsyi7.ewgTzci9gFmiBRk.578QMBS0UAsF4l.");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$UBUjYJioHURYHsVmW/cv5OuZR3lBoimNI1rTzfNJW4MKE8nO2wqzm");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "PasswordHash",
                value: "$2a$11$9Lnxk8r5qmVAzEIt3eOgUu9fEiZNNOboXTS7Zejukj/7PYx7Y3Omq");
        }
    }
}
