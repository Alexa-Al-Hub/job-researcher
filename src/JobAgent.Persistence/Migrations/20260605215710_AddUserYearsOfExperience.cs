using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobAgent.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserYearsOfExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "Users",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "Users");
        }
    }
}
