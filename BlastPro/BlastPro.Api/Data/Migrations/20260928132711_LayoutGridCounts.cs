using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlastPro.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LayoutGridCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LayoutColumns",
                table: "BlastProjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LayoutRows",
                table: "BlastProjects",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LayoutColumns",
                table: "BlastProjects");

            migrationBuilder.DropColumn(
                name: "LayoutRows",
                table: "BlastProjects");
        }
    }
}
