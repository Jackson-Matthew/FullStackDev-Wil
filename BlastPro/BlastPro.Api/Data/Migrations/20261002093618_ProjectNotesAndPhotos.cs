using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlastPro.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProjectNotesAndPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlastProjectId = table.Column<int>(type: "int", nullable: false),
                    AuthorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectNotes_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectNotes_BlastProjects_BlastProjectId",
                        column: x => x.BlastProjectId,
                        principalTable: "BlastProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectNoteComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectNoteId = table.Column<int>(type: "int", nullable: false),
                    AuthorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectNoteComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectNoteComments_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectNoteComments_ProjectNotes_ProjectNoteId",
                        column: x => x.ProjectNoteId,
                        principalTable: "ProjectNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectNotePhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectNoteId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectNotePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectNotePhotos_ProjectNotes_ProjectNoteId",
                        column: x => x.ProjectNoteId,
                        principalTable: "ProjectNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNoteComments_AuthorId",
                table: "ProjectNoteComments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNoteComments_ProjectNoteId",
                table: "ProjectNoteComments",
                column: "ProjectNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotePhotos_ProjectNoteId",
                table: "ProjectNotePhotos",
                column: "ProjectNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotes_AuthorId",
                table: "ProjectNotes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotes_BlastProjectId_UpdatedAtUtc",
                table: "ProjectNotes",
                columns: new[] { "BlastProjectId", "UpdatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectNoteComments");

            migrationBuilder.DropTable(
                name: "ProjectNotePhotos");

            migrationBuilder.DropTable(
                name: "ProjectNotes");
        }
    }
}
