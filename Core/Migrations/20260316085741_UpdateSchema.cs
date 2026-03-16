using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportLogs_Products_ProductId",
                table: "ImportLogs");

            migrationBuilder.DropIndex(
                name: "IX_ImportLogs_ProductId",
                table: "ImportLogs");

            migrationBuilder.DropColumn(
                name: "IsDarkMode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ItemsPerPage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastVisitedPage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RememberLastSession",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ImportLogs");

            migrationBuilder.RenameColumn(
                name: "QuantityAdded",
                table: "ImportLogs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "ActualImportPrice",
                table: "ImportLogs",
                newName: "TotalAmount");

            migrationBuilder.AlterColumn<long>(
                name: "SalePrice",
                table: "Products",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "ImportPrice",
                table: "Products",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoSaved",
                table: "ImportLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ImportLogDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ImportLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityAdded = table.Column<int>(type: "integer", nullable: false),
                    ActualImportPrice = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportLogDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportLogDetails_ImportLogs_ImportLogId",
                        column: x => x.ImportLogId,
                        principalTable: "ImportLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportLogDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportLogDetails_ImportLogId",
                table: "ImportLogDetails",
                column: "ImportLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportLogDetails_ProductId",
                table: "ImportLogDetails",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportLogDetails");

            migrationBuilder.DropColumn(
                name: "IsAutoSaved",
                table: "ImportLogs");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "ImportLogs",
                newName: "ActualImportPrice");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "ImportLogs",
                newName: "QuantityAdded");

            migrationBuilder.AddColumn<bool>(
                name: "IsDarkMode",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ItemsPerPage",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastVisitedPage",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RememberLastSession",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<long>(
                name: "SalePrice",
                table: "Products",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ImportPrice",
                table: "Products",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "ImportLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ImportLogs_ProductId",
                table: "ImportLogs",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportLogs_Products_ProductId",
                table: "ImportLogs",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
