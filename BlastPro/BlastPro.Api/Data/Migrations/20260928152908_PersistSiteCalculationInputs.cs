using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlastPro.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersistSiteCalculationInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DelayWindowMilliseconds",
                table: "BlastProjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExclusionRadiusMetres",
                table: "BlastProjects",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FlyrockLaunchAngleDegrees",
                table: "BlastProjects",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FlyrockLaunchHeightMetres",
                table: "BlastProjects",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FlyrockLaunchSpeedMetresPerSecond",
                table: "BlastProjects",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PpvDecayExponent",
                table: "BlastProjects",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PpvSiteCoefficient",
                table: "BlastProjects",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReceptorDistanceMetres",
                table: "BlastProjects",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubdrillMetres",
                table: "BlastProjects",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DelayWindowMilliseconds",
                table: "BlastProjects");

            migrationBuilder.DropColumn(
                name: "ExclusionRadiusMetres",
                table: "BlastProjects");

            migrationBuilder.DropColumn(
                name: "FlyrockLaunchAngleDegrees",
                table: "BlastProjects");

            migrationBuilder.DropColumn(
                name: "FlyrockLaunchHeightMetres",
                table: "BlastProjects");

            migrationBuilder.DropColumn(
                name: "FlyrockLaunchSpeedMetresPerSecond",
                table: "BlastProjects");

            migrationBuilder.DropColumn(
                name: "PpvDecayExponent",
                table: "BlastProjects");

            migrationBuilder.DropColumn(
                name: "PpvSiteCoefficient",
                table: "BlastProjects");

            migrationBuilder.DropColumn(
                name: "ReceptorDistanceMetres",
                table: "BlastProjects");

            migrationBuilder.DropColumn(
                name: "SubdrillMetres",
                table: "BlastProjects");
        }
    }
}
