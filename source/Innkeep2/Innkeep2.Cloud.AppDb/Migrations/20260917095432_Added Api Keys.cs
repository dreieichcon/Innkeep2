using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innkeep2.Cloud.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class AddedApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedOrderDatabasePath",
                table: "InnkeepCloudSettings",
                type: "TEXT",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedOrderDatabasePath",
                table: "InnkeepCloudSettings");
        }
    }
}
