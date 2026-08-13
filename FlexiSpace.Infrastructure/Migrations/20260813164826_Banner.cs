using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FlexiSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Banner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PriorityLevels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationForBanner",
                table: "PriorityLevels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "PriorityLevels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "BannerId",
                table: "PictureURLs",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Banners",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DurationInDays = table.Column<int>(type: "integer", nullable: false),
                    ListingId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Banners_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PictureURLs_BannerId",
                table: "PictureURLs",
                column: "BannerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Banners_ListingId",
                table: "Banners",
                column: "ListingId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PictureURLs_Banners_BannerId",
                table: "PictureURLs",
                column: "BannerId",
                principalTable: "Banners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PictureURLs_Banners_BannerId",
                table: "PictureURLs");

            migrationBuilder.DropTable(
                name: "Banners");

            migrationBuilder.DropIndex(
                name: "IX_PictureURLs_BannerId",
                table: "PictureURLs");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PriorityLevels");

            migrationBuilder.DropColumn(
                name: "DurationForBanner",
                table: "PriorityLevels");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "PriorityLevels");

            migrationBuilder.DropColumn(
                name: "BannerId",
                table: "PictureURLs");
        }
    }
}
