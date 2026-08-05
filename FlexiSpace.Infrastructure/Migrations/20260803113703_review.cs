using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexiSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class review : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrimaryBookingRequests_Reviews_ReviewId1",
                table: "PrimaryBookingRequests");

            migrationBuilder.DropIndex(
                name: "IX_PrimaryBookingRequests_ReviewId1",
                table: "PrimaryBookingRequests");

            migrationBuilder.DropColumn(
                name: "ReviewId1",
                table: "PrimaryBookingRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ReviewId1",
                table: "PrimaryBookingRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrimaryBookingRequests_ReviewId1",
                table: "PrimaryBookingRequests",
                column: "ReviewId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PrimaryBookingRequests_Reviews_ReviewId1",
                table: "PrimaryBookingRequests",
                column: "ReviewId1",
                principalTable: "Reviews",
                principalColumn: "Id");
        }
    }
}
