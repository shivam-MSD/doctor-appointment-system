using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoctorAppointmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationLinkToClinic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationLink",
                table: "Clinics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserPushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    P256dh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Auth = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPushSubscriptions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPushSubscriptions");

            migrationBuilder.DropColumn(
                name: "LocationLink",
                table: "Clinics");
        }
    }
}
