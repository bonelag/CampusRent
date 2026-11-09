using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusRent.Server.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePendingLandlordRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_landlord_requests_pending_user",
                table: "landlord_requests",
                column: "user_id",
                unique: true,
                filter: "\"status\" = 'PENDING'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_landlord_requests_pending_user",
                table: "landlord_requests");
        }
    }
}
