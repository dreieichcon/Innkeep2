using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innkeep2.Cloud.Database.Migrations
{
    /// <inheritdoc />
    public partial class SettingsSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InnkeepCloudSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PretixOrganizerSlug = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    PretixEventSlug = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnkeepCloudSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InnkeepCloudSettings");
        }
    }
}
