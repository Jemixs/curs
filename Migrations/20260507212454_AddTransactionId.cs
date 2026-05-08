using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportClub.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "Subscriptions",
                type: "TEXT",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "Subscriptions");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$7K.x2aMtgKjlUTCHW5WukuHlTBk.udiKfYENuiliNEiv/eJLaH3l6");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$DdyT8.9KAhPev.x..O7e9eEziR2nTx1h4qbf3RkS1qZ68UDKQtbta");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "PasswordHash",
                value: "$2a$11$p9zWi0UYPQgILSYSnk4veuLPrAu42Z13vP63H0brNQ/drTETCFSUS");
        }
    }
}
