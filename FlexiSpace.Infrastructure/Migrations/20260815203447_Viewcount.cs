using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexiSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Viewcount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "viewCount",
                table: "Listings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "viewCount",
                table: "Listings");
        }
    }
}
