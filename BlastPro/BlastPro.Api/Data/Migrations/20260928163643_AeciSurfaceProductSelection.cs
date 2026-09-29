using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlastPro.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AeciSurfaceProductSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultAeciProductCode",
                table: "BlastProjects",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AeciProductCode",
                table: "BlastHoles",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ProductDensityGramsPerCc",
                table: "BlastHoles",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultAeciProductCode",
                table: "BlastProjects");

            migrationBuilder.DropColumn(
                name: "AeciProductCode",
                table: "BlastHoles");

            migrationBuilder.DropColumn(
                name: "ProductDensityGramsPerCc",
                table: "BlastHoles");
        }
    }
}
