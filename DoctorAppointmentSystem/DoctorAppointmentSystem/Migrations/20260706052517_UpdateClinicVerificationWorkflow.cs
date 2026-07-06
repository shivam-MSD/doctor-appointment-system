using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoctorAppointmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClinicVerificationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Clinics");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Clinics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationStatus",
                table: "Clinics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "Clinics");

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Clinics",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
