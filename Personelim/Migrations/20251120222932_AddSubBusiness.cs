using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Personelim.Migrations
{
    /// <inheritdoc />
    public partial class AddSubBusiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Businesses_Users_OwnerId",
                table: "Businesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Businesses_Users_UserId",
                table: "Businesses");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessMembers_Businesses_BusinessId",
                table: "BusinessMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessMembers_Users_UserId",
                table: "BusinessMembers");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Businesses",
                newName: "ParentBusinessId");

            migrationBuilder.RenameIndex(
                name: "IX_Businesses_UserId",
                table: "Businesses",
                newName: "IX_Businesses_ParentBusinessId");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Businesses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Businesses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Businesses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Code",
                table: "PasswordResetTokens",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Businesses_Businesses_ParentBusinessId",
                table: "Businesses",
                column: "ParentBusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Businesses_Users_OwnerId",
                table: "Businesses",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessMembers_Businesses_BusinessId",
                table: "BusinessMembers",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessMembers_Users_UserId",
                table: "BusinessMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Businesses_Businesses_ParentBusinessId",
                table: "Businesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Businesses_Users_OwnerId",
                table: "Businesses");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessMembers_Businesses_BusinessId",
                table: "BusinessMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessMembers_Users_UserId",
                table: "BusinessMembers");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_Code",
                table: "PasswordResetTokens");

            migrationBuilder.RenameColumn(
                name: "ParentBusinessId",
                table: "Businesses",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Businesses_ParentBusinessId",
                table: "Businesses",
                newName: "IX_Businesses_UserId");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Businesses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Businesses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Businesses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddForeignKey(
                name: "FK_Businesses_Users_OwnerId",
                table: "Businesses",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Businesses_Users_UserId",
                table: "Businesses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessMembers_Businesses_BusinessId",
                table: "BusinessMembers",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessMembers_Users_UserId",
                table: "BusinessMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
