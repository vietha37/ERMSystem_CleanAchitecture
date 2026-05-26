using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERMSystem.Infrastructure.Data.Migrations
{
    public partial class AddMfaToAppUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MfaEnabled",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MfaEnabledAt",
                table: "AppUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MfaSecretProtected",
                table: "AppUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MfaEnabled",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "MfaEnabledAt",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "MfaSecretProtected",
                table: "AppUsers");
        }
    }
}
