using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FlexiSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class contractEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MessageType",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContractSnapshot",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConversationId",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "Contracts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "DurationUnit",
                table: "Contracts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LesseeCardAddress",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LesseeCardIssuanceDate",
                table: "Contracts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "LesseeName",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LessorCardAddress",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LessorCardIssuanceDate",
                table: "Contracts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "LessorName",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContractVerification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<long>(type: "bigint", nullable: false),
                    CardNumber = table.Column<string>(type: "text", nullable: true),
                    ScannedName = table.Column<string>(type: "text", nullable: true),
                    IsMatched = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IPAddress = table.Column<string>(type: "text", nullable: true),
                    RawQRData = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractVerification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractVerification_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ConversationId",
                table: "Contracts",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_LesseeId",
                table: "Contracts",
                column: "LesseeId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_LessorId",
                table: "Contracts",
                column: "LessorId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractVerification_ContractId",
                table: "ContractVerification",
                column: "ContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Conversations_ConversationId",
                table: "Contracts",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Users_LesseeId",
                table: "Contracts",
                column: "LesseeId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Users_LessorId",
                table: "Contracts",
                column: "LessorId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Conversations_ConversationId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Users_LesseeId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Users_LessorId",
                table: "Contracts");

            migrationBuilder.DropTable(
                name: "ContractVerification");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_ConversationId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_LesseeId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_LessorId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ContractSnapshot",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "DurationUnit",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "LesseeCardAddress",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "LesseeCardIssuanceDate",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "LesseeName",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "LessorCardAddress",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "LessorCardIssuanceDate",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "LessorName",
                table: "Contracts");
        }
    }
}
