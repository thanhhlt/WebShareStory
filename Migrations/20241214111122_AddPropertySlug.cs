using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertySlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParrentCateId",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "ParrentCateId",
                table: "Categories",
                newName: "ParentCateId");

            migrationBuilder.RenameIndex(
                name: "IX_Categories_ParrentCateId",
                table: "Categories",
                newName: "IX_Categories_ParentCateId");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Posts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParentCateId",
                table: "Categories",
                column: "ParentCateId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParentCateId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "ParentCateId",
                table: "Categories",
                newName: "ParrentCateId");

            migrationBuilder.RenameIndex(
                name: "IX_Categories_ParentCateId",
                table: "Categories",
                newName: "IX_Categories_ParrentCateId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParrentCateId",
                table: "Categories",
                column: "ParrentCateId",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}
