using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobAgent.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Applications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoreReason",
                table: "Applications",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ScoreReason",
                table: "Applications");
        }
    }
}
