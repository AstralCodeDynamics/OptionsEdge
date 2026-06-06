using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptionsEdge.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubscriptionPlan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WalletBalance = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    AiCallsToday = table.Column<int>(type: "integer", nullable: false),
                    AiCallsResetAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIUsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Feature = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelUsed = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    CostUsd = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    WalletBefore = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    WalletAfter = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIUsageLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BacktestResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Strategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Parameters = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    WinRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TotalTrades = table.Column<int>(type: "integer", nullable: false),
                    NetPnl = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    MaxDrawdown = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    SharpeRatio = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ProfitFactor = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TradeLog = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacktestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BacktestResults_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ModelUsed = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InputTokens = table.Column<int>(type: "integer", nullable: true),
                    OutputTokens = table.Column<int>(type: "integer", nullable: true),
                    CostUsd = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Strike = table.Column<int>(type: "integer", nullable: false),
                    OptionType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Expiry = table.Column<DateOnly>(type: "date", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    StopLoss = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Target1 = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Target2 = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    SignalId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    ExitReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Signals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SignalType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OptionType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Strike = table.Column<int>(type: "integer", nullable: false),
                    Expiry = table.Column<DateOnly>(type: "date", nullable: false),
                    EntryLow = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    EntryHigh = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    StopLoss = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Target1 = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Target2 = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    RiskReward = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Rationale = table.Column<string>(type: "text", nullable: false),
                    MarketSnapshot = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    ModelUsed = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    CostUsd = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Signals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AlertType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alerts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIUsageLogs_UserId",
                table: "AIUsageLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_PositionId",
                table: "Alerts",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_UserId",
                table: "Alerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BacktestResults_UserId",
                table: "BacktestResults",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_UserId",
                table: "ChatMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_UserId",
                table: "Positions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_UserId",
                table: "Signals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIUsageLogs");

            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "BacktestResults");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "Signals");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
