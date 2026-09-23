using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafkaConsumerMicroService.Migrations
{
    /// <inheritdoc />
    public partial class indexUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MarketRecords_IsValid_ReceivedAt",
                table: "MarketRecords",
                columns: new[] { "IsValid", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketRecords_IsValid_ReceivedAt",
                table: "MarketRecords");
        }
    }
}
