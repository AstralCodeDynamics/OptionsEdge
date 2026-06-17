using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptionsEdge.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConsistencyCheckTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsistencyCheckRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    RunAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TotalChecks = table.Column<int>(type: "integer", nullable: false),
                    NeedsReviewCount = table.Column<int>(type: "integer", nullable: false),
                    CheckFailedCount = table.Column<int>(type: "integer", nullable: false),
                    EmailSent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsistencyCheckRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsistencyFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ConsistencyCheckRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Detail = table.Column<string>(type: "text", nullable: false),
                    SuggestedAction = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsistencyFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsistencyFindings_ConsistencyCheckRuns_ConsistencyCheckRu~",
                        column: x => x.ConsistencyCheckRunId,
                        principalTable: "ConsistencyCheckRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsistencyFindings_ConsistencyCheckRunId",
                table: "ConsistencyFindings",
                column: "ConsistencyCheckRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsistencyFindings");

            migrationBuilder.DropTable(
                name: "ConsistencyCheckRuns");
        }
    }
}
