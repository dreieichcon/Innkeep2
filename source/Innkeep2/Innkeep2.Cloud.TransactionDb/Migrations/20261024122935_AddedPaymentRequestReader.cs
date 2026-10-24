using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innkeep2.Cloud.TransactionDb.Migrations
{
    /// <inheritdoc />
    public partial class AddedPaymentRequestReader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MerchantCode",
                table: "PaymentRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReaderId",
                table: "PaymentRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MerchantCode",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "ReaderId",
                table: "PaymentRequests");
        }
    }
}
