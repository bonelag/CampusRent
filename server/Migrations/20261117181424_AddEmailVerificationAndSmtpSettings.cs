using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusRent.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationAndSmtpSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "email_verification_attempts",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "email_verification_code_hash",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "email_verification_expires_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "email_verification_sent_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_email_verified",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "smtp_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    use_ssl = table.Column<bool>(type: "boolean", nullable: false),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    from_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    from_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_smtp_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rooms_status_expires_at",
                table: "rooms",
                columns: new[] { "status", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "smtp_settings");

            migrationBuilder.DropIndex(
                name: "IX_rooms_status_expires_at",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "email_verification_attempts",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_verification_code_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_verification_expires_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_verification_sent_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_email_verified",
                table: "users");
        }
    }
}
