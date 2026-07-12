using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FlexiSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class verifyContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractVerification_Contracts_ContractId",
                table: "ContractVerification");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContractVerification",
                table: "ContractVerification");

            migrationBuilder.DropIndex(
                name: "IX_ContractVerification_ContractId",
                table: "ContractVerification");

            migrationBuilder.DropColumn(
                name: "Dob",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ListingPictures",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ContractVerification");

            migrationBuilder.DropColumn(
                name: "CardNumber",
                table: "ContractVerification");

            migrationBuilder.DropColumn(
                name: "IPAddress",
                table: "ContractVerification");

            migrationBuilder.DropColumn(
                name: "IsMatched",
                table: "ContractVerification");

            migrationBuilder.DropColumn(
                name: "RawQRData",
                table: "ContractVerification");

            migrationBuilder.DropColumn(
                name: "ScannedName",
                table: "ContractVerification");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "ContractVerification");

            migrationBuilder.RenameTable(
                name: "ContractVerification",
                newName: "ContractVerifications");

            migrationBuilder.AddColumn<string>(
                name: "CitizenIDNumber",
                table: "Profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfIssue",
                table: "Profiles",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "Dob",
                table: "Profiles",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "IdentityCardNumber",
                table: "Profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PermanentResidence",
                table: "Profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLesseeAgreed",
                table: "ContractVerifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLessorAgreed",
                table: "ContractVerifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LesseeIpAddress",
                table: "ContractVerifications",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LesseeSignatureData",
                table: "ContractVerifications",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LesseeSignedAt",
                table: "ContractVerifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LessorIpAddress",
                table: "ContractVerifications",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LessorSignatureData",
                table: "ContractVerifications",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LessorSignedAt",
                table: "ContractVerifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "ContractVerifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContractVerifications",
                table: "ContractVerifications",
                column: "ContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractVerifications_Contracts_ContractId",
                table: "ContractVerifications",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractVerifications_Contracts_ContractId",
                table: "ContractVerifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContractVerifications",
                table: "ContractVerifications");

            migrationBuilder.DropColumn(
                name: "CitizenIDNumber",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "DateOfIssue",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Dob",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "IdentityCardNumber",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "PermanentResidence",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "IsLesseeAgreed",
                table: "ContractVerifications");

            migrationBuilder.DropColumn(
                name: "IsLessorAgreed",
                table: "ContractVerifications");

            migrationBuilder.DropColumn(
                name: "LesseeIpAddress",
                table: "ContractVerifications");

            migrationBuilder.DropColumn(
                name: "LesseeSignatureData",
                table: "ContractVerifications");

            migrationBuilder.DropColumn(
                name: "LesseeSignedAt",
                table: "ContractVerifications");

            migrationBuilder.DropColumn(
                name: "LessorIpAddress",
                table: "ContractVerifications");

            migrationBuilder.DropColumn(
                name: "LessorSignatureData",
                table: "ContractVerifications");

            migrationBuilder.DropColumn(
                name: "LessorSignedAt",
                table: "ContractVerifications");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "ContractVerifications");

            migrationBuilder.RenameTable(
                name: "ContractVerifications",
                newName: "ContractVerification");

            migrationBuilder.AddColumn<DateTime>(
                name: "Dob",
                table: "Users",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<List<string>>(
                name: "ListingPictures",
                table: "Listings",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "ContractVerification",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                table: "ContractVerification",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IPAddress",
                table: "ContractVerification",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMatched",
                table: "ContractVerification",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RawQRData",
                table: "ContractVerification",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScannedName",
                table: "ContractVerification",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "ContractVerification",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContractVerification",
                table: "ContractVerification",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ContractVerification_ContractId",
                table: "ContractVerification",
                column: "ContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractVerification_Contracts_ContractId",
                table: "ContractVerification",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
