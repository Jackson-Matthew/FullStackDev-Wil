using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlastPro.Api.Data.Migrations;

public partial class BenchGeometryAndVibrationGuidance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "BenchLengthMetres", table: "BlastProjects", type: "decimal(18,4)",
            precision: 18, scale: 4, nullable: true);
        migrationBuilder.AddColumn<decimal>(
            name: "BenchWidthMetres", table: "BlastProjects", type: "decimal(18,4)",
            precision: 18, scale: 4, nullable: true);
        migrationBuilder.AddColumn<decimal>(
            name: "DominantFrequencyHz", table: "BlastProjects", type: "decimal(18,4)",
            precision: 18, scale: 4, nullable: true);
        migrationBuilder.AddColumn<decimal>(
            name: "DiameterMillimetres", table: "BlastHoles", type: "decimal(18,4)",
            precision: 18, scale: 4, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "PatternType", table: "BlastProjects", type: "nvarchar(20)",
            maxLength: 20, nullable: false, defaultValue: "Rectangular");
        migrationBuilder.AddColumn<string>(
            name: "ReferenceExplosiveFamily", table: "BlastProjects", type: "nvarchar(30)",
            maxLength: 30, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "ReceptorStructureType", table: "BlastProjects", type: "nvarchar(30)",
            maxLength: 30, nullable: false, defaultValue: "Unspecified");
        migrationBuilder.AddColumn<string>(
            name: "VibrationThresholdMode", table: "BlastProjects", type: "nvarchar(10)",
            maxLength: 10, nullable: false, defaultValue: "Manual");
        migrationBuilder.AddCheckConstraint(
            name: "CK_BlastProjects_BenchLength_Positive", table: "BlastProjects",
            sql: "[BenchLengthMetres] IS NULL OR [BenchLengthMetres] > 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_BlastProjects_BenchWidth_Positive", table: "BlastProjects",
            sql: "[BenchWidthMetres] IS NULL OR [BenchWidthMetres] > 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_BlastProjects_BenchLength_Positive", table: "BlastProjects");
        migrationBuilder.DropCheckConstraint(name: "CK_BlastProjects_BenchWidth_Positive", table: "BlastProjects");
        migrationBuilder.DropColumn(name: "BenchLengthMetres", table: "BlastProjects");
        migrationBuilder.DropColumn(name: "BenchWidthMetres", table: "BlastProjects");
        migrationBuilder.DropColumn(name: "DominantFrequencyHz", table: "BlastProjects");
        migrationBuilder.DropColumn(name: "DiameterMillimetres", table: "BlastHoles");
        migrationBuilder.DropColumn(name: "PatternType", table: "BlastProjects");
        migrationBuilder.DropColumn(name: "ReferenceExplosiveFamily", table: "BlastProjects");
        migrationBuilder.DropColumn(name: "ReceptorStructureType", table: "BlastProjects");
        migrationBuilder.DropColumn(name: "VibrationThresholdMode", table: "BlastProjects");
    }
}
