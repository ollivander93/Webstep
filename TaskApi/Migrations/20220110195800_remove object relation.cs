using Microsoft.EntityFrameworkCore.Migrations;

namespace TaskApi.Migrations
{
    public partial class removeobjectrelation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskLists_AspNetUsers_OwnerId",
                table: "TaskLists");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_AspNetUsers_OwnerId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_OwnerId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_TaskLists_OwnerId",
                table: "TaskLists");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tasks_OwnerId",
                table: "Tasks",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskLists_OwnerId",
                table: "TaskLists",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskLists_AspNetUsers_OwnerId",
                table: "TaskLists",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_AspNetUsers_OwnerId",
                table: "Tasks",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
