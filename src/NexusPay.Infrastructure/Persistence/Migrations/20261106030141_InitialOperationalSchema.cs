using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusPay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOperationalSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerCodeNormalized = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    EmailNormalized = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DocumentNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DocumentNumberNormalized = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationCodeNormalized = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "char(3)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastProcessIdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastProviderCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastProviderReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastFailureCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ProcessingAttempts = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialOperations", x => x.Id);
                    table.CheckConstraint("CK_FinancialOperations_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_FinancialOperations_Currency", "[Currency] IN ('PEN', 'USD', 'EUR')");
                    table.ForeignKey(
                        name: "FK_FinancialOperations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperationStatusHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationStatusHistory_FinancialOperations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "FinancialOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProviderReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAttempts_FinancialOperations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "FinancialOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Customers_CustomerCode",
                table: "Customers",
                column: "CustomerCodeNormalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Customers_Document",
                table: "Customers",
                columns: new[] { "DocumentType", "DocumentNumberNormalized" },
                unique: true,
                filter: "[DocumentNumberNormalized] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Customers_EmailNormalized",
                table: "Customers",
                column: "EmailNormalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOperations_CreatedAtUtc_Id",
                table: "FinancialOperations",
                columns: new[] { "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOperations_CustomerId_CreatedAtUtc",
                table: "FinancialOperations",
                columns: new[] { "CustomerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOperations_Status_CreatedAtUtc",
                table: "FinancialOperations",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_FinancialOperations_Code",
                table: "FinancialOperations",
                column: "OperationCodeNormalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_FinancialOperations_IdempotencyKey",
                table: "FinancialOperations",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationStatusHistory_OperationId_ChangedAtUtc",
                table: "OperationStatusHistory",
                columns: new[] { "OperationId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_IdempotencyKey",
                table: "PaymentAttempts",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_OperationId_AttemptNumber",
                table: "PaymentAttempts",
                columns: new[] { "OperationId", "AttemptNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationStatusHistory");

            migrationBuilder.DropTable(
                name: "PaymentAttempts");

            migrationBuilder.DropTable(
                name: "FinancialOperations");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
