using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningResourcesApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leermiddelen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Titel = table.Column<string>(type: "TEXT", nullable: false),
                    Beschrijving = table.Column<string>(type: "TEXT", nullable: false),
                    Link = table.Column<string>(type: "TEXT", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leermiddelen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reacties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GebruikerId = table.Column<string>(type: "TEXT", nullable: false),
                    Gebruikersnaam = table.Column<string>(type: "TEXT", nullable: false),
                    Tekst = table.Column<string>(type: "TEXT", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LeermiddelId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reacties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reacties_Leermiddelen_LeermiddelId",
                        column: x => x.LeermiddelId,
                        principalTable: "Leermiddelen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leermiddelen_AangemaaktOp",
                table: "Leermiddelen",
                column: "AangemaaktOp");

            migrationBuilder.CreateIndex(
                name: "IX_Reacties_AangemaaktOp",
                table: "Reacties",
                column: "AangemaaktOp");

            migrationBuilder.CreateIndex(
                name: "IX_Reacties_LeermiddelId",
                table: "Reacties",
                column: "LeermiddelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reacties");

            migrationBuilder.DropTable(
                name: "Leermiddelen");
        }
    }
}
