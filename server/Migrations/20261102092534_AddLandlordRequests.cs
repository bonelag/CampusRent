using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusRent.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLandlordRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "landlord_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    image_urls = table.Column<string>(type: "text", nullable: false),
                    pdf_url = table.Column<string>(type: "text", nullable: true),
                    pdf_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    admin_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_landlord_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_landlord_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_landlord_requests_status",
                table: "landlord_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_landlord_requests_user_id",
                table: "landlord_requests",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "landlord_requests");
        }
    }
}
