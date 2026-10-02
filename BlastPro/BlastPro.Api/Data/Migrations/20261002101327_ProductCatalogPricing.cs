using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlastPro.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProductCatalogPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AeciProductCode",
                table: "ExplosiveProducts",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExplosiveProducts_CompanyId_AeciProductCode",
                table: "ExplosiveProducts",
                columns: new[] { "CompanyId", "AeciProductCode" },
                unique: true,
                filter: "[AeciProductCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExplosiveProducts_CompanyId_AeciProductCode",
                table: "ExplosiveProducts");

            migrationBuilder.DropColumn(
                name: "AeciProductCode",
                table: "ExplosiveProducts");
        }
    }
}
