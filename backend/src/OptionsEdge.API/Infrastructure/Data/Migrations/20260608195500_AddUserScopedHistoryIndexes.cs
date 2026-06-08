using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OptionsEdge.API.Infrastructure.Data;

#nullable disable

namespace OptionsEdge.API.Infrastructure.Data.Migrations;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20260608195500_AddUserScopedHistoryIndexes")]
public partial class AddUserScopedHistoryIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_BacktestResults_UserId_CreatedAt",
            table: "BacktestResults",
            columns: new[] { "UserId", "CreatedAt" },
            descending: new[] { false, true });

        migrationBuilder.CreateIndex(
            name: "IX_ChatMessages_UserId_SessionId_CreatedAt",
            table: "ChatMessages",
            columns: new[] { "UserId", "SessionId", "CreatedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_BacktestResults_UserId_CreatedAt",
            table: "BacktestResults");

        migrationBuilder.DropIndex(
            name: "IX_ChatMessages_UserId_SessionId_CreatedAt",
            table: "ChatMessages");
    }
}
