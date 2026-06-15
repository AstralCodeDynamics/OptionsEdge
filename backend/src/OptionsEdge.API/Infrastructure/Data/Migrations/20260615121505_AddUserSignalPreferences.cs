using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptionsEdge.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSignalPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSignalPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NiftyAutoSignalEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NiftyAutoSignalTimes = table.Column<string>(type: "text", nullable: false),
                    BankNiftyAutoSignalEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    BankNiftyAutoSignalTimes = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSignalPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSignalPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSignalPreferences_UserId",
                table: "UserSignalPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSignalPreferences");
        }
    }
}
