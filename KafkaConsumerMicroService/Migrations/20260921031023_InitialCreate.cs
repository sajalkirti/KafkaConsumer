using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafkaConsumerMicroService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountNumber = table.Column<long>(type: "bigint", nullable: false),
                    PnLAmount = table.Column<double>(type: "float", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketRecords_AccountNumber",
                table: "MarketRecords",
                column: "AccountNumber");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRecords_IsValid",
                table: "MarketRecords",
                column: "IsValid");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRecords_ReceivedAt",
                table: "MarketRecords",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketRecords_SourceSystem",
                table: "MarketRecords",
                column: "SourceSystem");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketRecords");
        }
    }
}
