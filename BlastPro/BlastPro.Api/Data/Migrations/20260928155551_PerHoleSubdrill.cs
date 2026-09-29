using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlastPro.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PerHoleSubdrill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SubdrillMetres",
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
                name: "SubdrillMetres",
                table: "BlastHoles");
        }
    }
}
