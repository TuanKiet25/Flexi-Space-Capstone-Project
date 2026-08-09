using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexiSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SpacePart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ParentSpaceId",
                table: "Spaces",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Spaces_ParentSpaceId",
                table: "Spaces",
                column: "ParentSpaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Spaces_Spaces_ParentSpaceId",
                table: "Spaces",
                column: "ParentSpaceId",
                principalTable: "Spaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Spaces_Spaces_ParentSpaceId",
                table: "Spaces");

            migrationBuilder.DropIndex(
                name: "IX_Spaces_ParentSpaceId",
                table: "Spaces");

            migrationBuilder.DropColumn(
                name: "ParentSpaceId",
                table: "Spaces");
        }
    }
}
