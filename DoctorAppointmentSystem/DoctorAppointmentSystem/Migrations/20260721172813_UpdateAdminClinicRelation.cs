using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoctorAppointmentSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminClinicRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Admins_Clinics_ClinicId",
                table: "Admins");

            migrationBuilder.DropIndex(
                name: "IX_Admins_ClinicId",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "Admins");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPasswordChange",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AdminClinics",
                columns: table => new
                {
                    AdminClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminClinics", x => x.AdminClinicId);
                    table.ForeignKey(
                        name: "FK_AdminClinics_Admins_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Admins",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminClinics_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "ClinicId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminClinics_AdminId",
                table: "AdminClinics",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminClinics_ClinicId",
                table: "AdminClinics",
                column: "ClinicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminClinics");

            migrationBuilder.DropColumn(
                name: "RequiresPasswordChange",
                table: "Users");

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "Admins",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Admins_ClinicId",
                table: "Admins",
                column: "ClinicId");

            migrationBuilder.AddForeignKey(
                name: "FK_Admins_Clinics_ClinicId",
                table: "Admins",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "ClinicId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
