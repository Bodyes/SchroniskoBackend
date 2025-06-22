using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schronisko.Migrations
{
    /// <inheritdoc />
    public partial class AddPostCommentRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentsId",
                table: "Post");

            migrationBuilder.AddColumn<int>(
                name: "PostId",
                table: "Comment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Comment_PostId",
                table: "Comment",
                column: "PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Post_PostId",
                table: "Comment",
                column: "PostId",
                principalTable: "Post",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Post_PostId",
                table: "Comment");

            migrationBuilder.DropIndex(
                name: "IX_Comment_PostId",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "PostId",
                table: "Comment");

            migrationBuilder.AddColumn<int>(
                name: "CommentsId",
                table: "Post",
                type: "integer",
                nullable: true);
        }
    }
}
