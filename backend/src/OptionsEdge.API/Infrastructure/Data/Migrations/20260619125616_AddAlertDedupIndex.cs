using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptionsEdge.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertDedupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alerts_PositionId",
                table: "Alerts");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_PositionId_AlertType_CreatedAt",
                table: "Alerts",
                columns: new[] { "PositionId", "AlertType", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alerts_PositionId_AlertType_CreatedAt",
                table: "Alerts");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_PositionId",
                table: "Alerts",
                column: "PositionId");
        }
    }
}
