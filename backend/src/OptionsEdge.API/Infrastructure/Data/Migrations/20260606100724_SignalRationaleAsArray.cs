using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptionsEdge.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SignalRationaleAsArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string[]>(
                name: "Rationale",
                table: "Signals",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Rationale",
                table: "Signals",
                type: "text",
                nullable: false,
                oldClrType: typeof(string[]),
                oldType: "text[]");
        }
    }
}
