using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innkeep2.Cloud.TransactionDb.Migrations
{
    /// <inheritdoc />
    public partial class AddedSumupTransactionCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SumupTransactionCode",
                table: "Transactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SumupTransactionCode",
                table: "PaymentRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TerminationReason",
                table: "PaymentRequests",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SumupTransactionCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SumupTransactionCode",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "TerminationReason",
                table: "PaymentRequests");
        }
    }
}
