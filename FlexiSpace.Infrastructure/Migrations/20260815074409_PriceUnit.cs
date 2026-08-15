using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexiSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PriceUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriceUnit",
                table: "Listings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceUnit",
                table: "Listings");
        }
    }
}
