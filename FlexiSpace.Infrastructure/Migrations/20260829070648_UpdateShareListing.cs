using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexiSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShareListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SharedSpaceAmenities_Amenities_AmenityId",
                table: "SharedSpaceAmenities");

            migrationBuilder.DropForeignKey(
                name: "FK_ShareSpaceCategory_BussinessCategories_BussinessCategoryId",
                table: "ShareSpaceCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionHistories_Wallet_WalletId",
                table: "TransactionHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Wallet_WalletId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Wallet_Users_UserId",
                table: "Wallet");

            migrationBuilder.DropIndex(
                name: "IX_ShareSpaceCategory_BussinessCategoryId",
                table: "ShareSpaceCategory");

            migrationBuilder.DropIndex(
                name: "IX_SharedSpaceAmenities_AmenityId",
                table: "SharedSpaceAmenities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Wallet",
                table: "Wallet");

            migrationBuilder.DropColumn(
                name: "BussinessCategoryId",
                table: "ShareSpaceCategory");

            migrationBuilder.DropColumn(
                name: "AmenityId",
                table: "SharedSpaceAmenities");

            migrationBuilder.RenameTable(
                name: "Wallet",
                newName: "Wallets");

            migrationBuilder.RenameIndex(
                name: "IX_Wallet_UserId",
                table: "Wallets",
                newName: "IX_Wallets_UserId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ShareSpaceCategory",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ShareSpaceCategory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ShareSpaceCategory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ShareSpaceCategory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ShareSpaceCategory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ShareSpaceCategory",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ShareSpaceCategory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SharedSpaceAmenities",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SharedSpaceAmenities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SharedSpaceAmenities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SharedSpaceAmenities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SharedSpaceAmenities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "SharedSpaceAmenities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SharedSpaceAmenities",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "SharedSpaceAmenities",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "Wallets",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Wallets",
                table: "Wallets",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionHistories_Wallets_WalletId",
                table: "TransactionHistories",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Wallets_WalletId",
                table: "Transactions",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_Users_UserId",
                table: "Wallets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionHistories_Wallets_WalletId",
                table: "TransactionHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Wallets_WalletId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_Users_UserId",
                table: "Wallets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Wallets",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ShareSpaceCategory");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ShareSpaceCategory");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ShareSpaceCategory");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ShareSpaceCategory");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ShareSpaceCategory");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ShareSpaceCategory");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ShareSpaceCategory");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SharedSpaceAmenities");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SharedSpaceAmenities");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SharedSpaceAmenities");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SharedSpaceAmenities");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SharedSpaceAmenities");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "SharedSpaceAmenities");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SharedSpaceAmenities");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "SharedSpaceAmenities");

            migrationBuilder.RenameTable(
                name: "Wallets",
                newName: "Wallet");

            migrationBuilder.RenameIndex(
                name: "IX_Wallets_UserId",
                table: "Wallet",
                newName: "IX_Wallet_UserId");

            migrationBuilder.AddColumn<long>(
                name: "BussinessCategoryId",
                table: "ShareSpaceCategory",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "AmenityId",
                table: "SharedSpaceAmenities",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "Wallet",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Wallet",
                table: "Wallet",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ShareSpaceCategory_BussinessCategoryId",
                table: "ShareSpaceCategory",
                column: "BussinessCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedSpaceAmenities_AmenityId",
                table: "SharedSpaceAmenities",
                column: "AmenityId");

            migrationBuilder.AddForeignKey(
                name: "FK_SharedSpaceAmenities_Amenities_AmenityId",
                table: "SharedSpaceAmenities",
                column: "AmenityId",
                principalTable: "Amenities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShareSpaceCategory_BussinessCategories_BussinessCategoryId",
                table: "ShareSpaceCategory",
                column: "BussinessCategoryId",
                principalTable: "BussinessCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionHistories_Wallet_WalletId",
                table: "TransactionHistories",
                column: "WalletId",
                principalTable: "Wallet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Wallet_WalletId",
                table: "Transactions",
                column: "WalletId",
                principalTable: "Wallet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Wallet_Users_UserId",
                table: "Wallet",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
