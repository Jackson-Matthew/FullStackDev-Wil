using BlastPro.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlastPro.Api.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260924220000_SaveCalculationResults")]
public partial class SaveCalculationResults : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "TotalCost",
            table: "CalculationResults",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldPrecision: 18,
            oldScale: 2);

        migrationBuilder.AddColumn<string>(
            name: "CurrencyCode",
            table: "CalculationResults",
            type: "nvarchar(3)",
            maxLength: 3,
            nullable: true);

        migrationBuilder.DropIndex(
            name: "IX_CalculationResults_BlastProjectId_IsCurrent",
            table: "CalculationResults");

        migrationBuilder.CreateIndex(
            name: "IX_CalculationResults_BlastProjectId_IsCurrent",
            table: "CalculationResults",
            columns: new[] { "BlastProjectId", "IsCurrent" },
            unique: true,
            filter: "[IsCurrent] = 1");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CalculationResults_BlastProjectId_IsCurrent",
            table: "CalculationResults");

        migrationBuilder.DropColumn(
            name: "CurrencyCode",
            table: "CalculationResults");

        migrationBuilder.AlterColumn<decimal>(
            name: "TotalCost",
            table: "CalculationResults",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldPrecision: 18,
            oldScale: 2,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_CalculationResults_BlastProjectId_IsCurrent",
            table: "CalculationResults",
            columns: new[] { "BlastProjectId", "IsCurrent" });
    }
}
