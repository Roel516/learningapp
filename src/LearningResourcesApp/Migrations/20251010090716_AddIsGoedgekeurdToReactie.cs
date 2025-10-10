using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningResourcesApp.Migrations
{
    /// <inheritdoc />
    public partial class AddIsGoedgekeurdToReactie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGoedgekeurd",
                table: "Reacties",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGoedgekeurd",
                table: "Reacties");
        }
    }
}
