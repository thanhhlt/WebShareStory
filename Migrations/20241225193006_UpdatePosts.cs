using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Images_PostId",
                table: "Images");

            migrationBuilder.CreateIndex(
                name: "IX_Images_PostId",
                table: "Images",
                column: "PostId",
                unique: true,
                filter: "[PostId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Images_PostId",
                table: "Images");

            migrationBuilder.CreateIndex(
                name: "IX_Images_PostId",
                table: "Images",
                column: "PostId");
        }
    }
}
