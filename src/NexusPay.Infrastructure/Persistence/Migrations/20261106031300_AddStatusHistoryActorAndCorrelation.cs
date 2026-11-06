using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusPay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusHistoryActorAndCorrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorUserId",
                table: "OperationStatusHistory",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "OperationStatusHistory",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorUserId",
                table: "OperationStatusHistory");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "OperationStatusHistory");
        }
    }
}
