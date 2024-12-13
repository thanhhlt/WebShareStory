using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserRelation2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRelations_Users_OtherUserId",
                table: "UserRelations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRelations_Users_UserId",
                table: "UserRelations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRelations",
                table: "UserRelations");

            migrationBuilder.DropIndex(
                name: "IX_UserRelations_UserId",
                table: "UserRelations");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "UserRelations");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserRelations",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OtherUserId",
                table: "UserRelations",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRelations",
                table: "UserRelations",
                columns: new[] { "UserId", "OtherUserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserRelations_Users_OtherUserId",
                table: "UserRelations",
                column: "OtherUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRelations_Users_UserId",
                table: "UserRelations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRelations_Users_OtherUserId",
                table: "UserRelations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRelations_Users_UserId",
                table: "UserRelations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRelations",
                table: "UserRelations");

            migrationBuilder.AlterColumn<string>(
                name: "OtherUserId",
                table: "UserRelations",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserRelations",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "UserRelations",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRelations",
                table: "UserRelations",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserRelations_UserId",
                table: "UserRelations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRelations_Users_OtherUserId",
                table: "UserRelations",
                column: "OtherUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRelations_Users_UserId",
                table: "UserRelations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
