using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WIMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClientFeedback_v1_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostCenter",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "CostCenter",
                table: "Employees");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DocumentDate",
                table: "Vouchers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoContentType",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhotoData",
                table: "AspNetUsers",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentDate",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "PhotoContentType",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "CostCenter",
                table: "Vouchers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCenter",
                table: "Employees",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }
    }
}
