using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class AddGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Group_Users_CuratorId",
                table: "Group");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Group_StudentGroupId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Group",
                table: "Group");

            migrationBuilder.RenameTable(
                name: "Group",
                newName: "Groups");

            migrationBuilder.RenameIndex(
                name: "IX_Group_CuratorId",
                table: "Groups",
                newName: "IX_Groups_CuratorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Groups",
                table: "Groups",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Users_CuratorId",
                table: "Groups",
                column: "CuratorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Groups_StudentGroupId",
                table: "Users",
                column: "StudentGroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Users_CuratorId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Groups_StudentGroupId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Groups",
                table: "Groups");

            migrationBuilder.RenameTable(
                name: "Groups",
                newName: "Group");

            migrationBuilder.RenameIndex(
                name: "IX_Groups_CuratorId",
                table: "Group",
                newName: "IX_Group_CuratorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Group",
                table: "Group",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Group_Users_CuratorId",
                table: "Group",
                column: "CuratorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Group_StudentGroupId",
                table: "Users",
                column: "StudentGroupId",
                principalTable: "Group",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
