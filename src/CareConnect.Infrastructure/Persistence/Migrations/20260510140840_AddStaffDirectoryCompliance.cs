using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffDirectoryCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Acknowledgements_InformationUpdateId_DepartmentId_StaffMemb~",
                table: "Acknowledgements");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpiresOn",
                table: "InformationUpdates",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReviewBy",
                table: "InformationUpdates",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectionNote",
                table: "Acknowledgements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "Acknowledgements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedStaffMemberName",
                table: "Acknowledgements",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "StaffMemberId",
                table: "Acknowledgements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Acknowledgements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VoidedAt",
                table: "Acknowledgements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoidedByUserId",
                table: "Acknowledgements",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Acknowledgements"
                SET "NormalizedStaffMemberName" = UPPER(regexp_replace(btrim("StaffMemberName"), '\s+', ' ', 'g'))
                WHERE "NormalizedStaffMemberName" = '';
                """);

            migrationBuilder.CreateTable(
                name: "StaffMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EmployeeReference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffMembers_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Acknowledgements_InformationUpdateId_DepartmentId_Normalize~",
                table: "Acknowledgements",
                columns: new[] { "InformationUpdateId", "DepartmentId", "NormalizedStaffMemberName" });

            migrationBuilder.CreateIndex(
                name: "IX_Acknowledgements_StaffMemberId",
                table: "Acknowledgements",
                column: "StaffMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_DepartmentId_NormalizedName",
                table: "StaffMembers",
                columns: new[] { "DepartmentId", "NormalizedName" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_Acknowledgements_StaffMembers_StaffMemberId",
                table: "Acknowledgements",
                column: "StaffMemberId",
                principalTable: "StaffMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Acknowledgements_StaffMembers_StaffMemberId",
                table: "Acknowledgements");

            migrationBuilder.DropTable(
                name: "StaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_Acknowledgements_InformationUpdateId_DepartmentId_Normalize~",
                table: "Acknowledgements");

            migrationBuilder.DropIndex(
                name: "IX_Acknowledgements_StaffMemberId",
                table: "Acknowledgements");

            migrationBuilder.DropColumn(
                name: "ExpiresOn",
                table: "InformationUpdates");

            migrationBuilder.DropColumn(
                name: "ReviewBy",
                table: "InformationUpdates");

            migrationBuilder.DropColumn(
                name: "CorrectionNote",
                table: "Acknowledgements");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "Acknowledgements");

            migrationBuilder.DropColumn(
                name: "NormalizedStaffMemberName",
                table: "Acknowledgements");

            migrationBuilder.DropColumn(
                name: "StaffMemberId",
                table: "Acknowledgements");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Acknowledgements");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "Acknowledgements");

            migrationBuilder.DropColumn(
                name: "VoidedByUserId",
                table: "Acknowledgements");

            migrationBuilder.CreateIndex(
                name: "IX_Acknowledgements_InformationUpdateId_DepartmentId_StaffMemb~",
                table: "Acknowledgements",
                columns: new[] { "InformationUpdateId", "DepartmentId", "StaffMemberName" });
        }
    }
}
