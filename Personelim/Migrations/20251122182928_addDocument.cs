using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Personelim.Migrations
{
    /// <inheritdoc />
    public partial class addDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Salary",
                table: "BusinessMembers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TCIdentityNumber",
                table: "BusinessMembers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemberDocument",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileExtension = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberDocument_BusinessMembers_BusinessMemberId",
                        column: x => x.BusinessMemberId,
                        principalTable: "BusinessMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberDocument_BusinessMemberId",
                table: "MemberDocument",
                column: "BusinessMemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberDocument");

            migrationBuilder.DropColumn(
                name: "Salary",
                table: "BusinessMembers");

            migrationBuilder.DropColumn(
                name: "TCIdentityNumber",
                table: "BusinessMembers");
        }
    }
}
