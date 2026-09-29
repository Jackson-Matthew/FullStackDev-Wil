using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlastPro.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LiveTimingSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimingIntervalMilliseconds",
                table: "BlastProjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimingOrder",
                table: "BlastProjects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Rows");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimingIntervalMilliseconds",
                table: "BlastProjects");

            migrationBuilder.DropColumn(
                name: "TimingOrder",
                table: "BlastProjects");
        }
    }
}
