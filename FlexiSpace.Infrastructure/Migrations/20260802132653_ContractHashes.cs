using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexiSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContractHashes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PostSignHash",
                table: "Contracts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostSignSnapshot",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreSignHash",
                table: "Contracts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreSignSnapshot",
                table: "Contracts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostSignHash",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "PostSignSnapshot",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "PreSignHash",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "PreSignSnapshot",
                table: "Contracts");
        }
    }
}
