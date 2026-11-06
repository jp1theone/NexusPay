using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusPay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProvidersAuditAndIntegrationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentProviderId",
                table: "PaymentAttempts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChangeSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DurationMilliseconds = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    TechnicalDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationLogs_PaymentAttempts_PaymentAttemptId",
                        column: x => x.PaymentAttemptId,
                        principalTable: "PaymentAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationLogs_PaymentProviders_PaymentProviderId",
                        column: x => x.PaymentProviderId,
                        principalTable: "PaymentProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PaymentProviders",
                columns: new[] { "Id", "ConfigurationJson", "CreatedAtUtc", "IsAvailable", "IsEnabled", "Name", "ProviderCode", "Type", "UpdatedAtUtc" },
                values: new object[] { new Guid("c6e7d319-868b-4a11-929d-a51d6d0d5e10"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, true, "Nexus Provider Alpha", "NEXUSPROVIDERALPHA", "Simulator", null });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_PaymentProviderId",
                table: "PaymentAttempts",
                column: "PaymentProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId_OccurredAtUtc",
                table: "AuditLogs",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAtUtc",
                table: "AuditLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_CorrelationId",
                table: "IntegrationLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_PaymentAttemptId",
                table: "IntegrationLogs",
                column: "PaymentAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_PaymentProviderId_OccurredAtUtc",
                table: "IntegrationLogs",
                columns: new[] { "PaymentProviderId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviders_ProviderCode",
                table: "PaymentProviders",
                column: "ProviderCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAttempts_PaymentProviders_PaymentProviderId",
                table: "PaymentAttempts",
                column: "PaymentProviderId",
                principalTable: "PaymentProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAttempts_PaymentProviders_PaymentProviderId",
                table: "PaymentAttempts");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "IntegrationLogs");

            migrationBuilder.DropTable(
                name: "PaymentProviders");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_PaymentProviderId",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "PaymentProviderId",
                table: "PaymentAttempts");
        }
    }
}
