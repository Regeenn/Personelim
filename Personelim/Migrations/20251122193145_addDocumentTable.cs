using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Personelim.Migrations
{
    /// <inheritdoc />
    public partial class addDocumentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberDocument_BusinessMembers_BusinessMemberId",
                table: "MemberDocument");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MemberDocument",
                table: "MemberDocument");

            migrationBuilder.RenameTable(
                name: "MemberDocument",
                newName: "MemberDocuments");

            migrationBuilder.RenameIndex(
                name: "IX_MemberDocument_BusinessMemberId",
                table: "MemberDocuments",
                newName: "IX_MemberDocuments_BusinessMemberId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MemberDocuments",
                table: "MemberDocuments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberDocuments_BusinessMembers_BusinessMemberId",
                table: "MemberDocuments",
                column: "BusinessMemberId",
                principalTable: "BusinessMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberDocuments_BusinessMembers_BusinessMemberId",
                table: "MemberDocuments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MemberDocuments",
                table: "MemberDocuments");

            migrationBuilder.RenameTable(
                name: "MemberDocuments",
                newName: "MemberDocument");

            migrationBuilder.RenameIndex(
                name: "IX_MemberDocuments_BusinessMemberId",
                table: "MemberDocument",
                newName: "IX_MemberDocument_BusinessMemberId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MemberDocument",
                table: "MemberDocument",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberDocument_BusinessMembers_BusinessMemberId",
                table: "MemberDocument",
                column: "BusinessMemberId",
                principalTable: "BusinessMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
