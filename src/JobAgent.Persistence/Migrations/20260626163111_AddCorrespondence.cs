using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobAgent.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrespondence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Correspondence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Mailbox = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    ExternalMessageId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ThreadId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ApplicationId = table.Column<int>(type: "INTEGER", nullable: true),
                    FromAddress = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    MatchConfidence = table.Column<int>(type: "INTEGER", nullable: false),
                    SuggestedReply = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Correspondence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Correspondence_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Correspondence_ApplicationId",
                table: "Correspondence",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Correspondence_Mailbox_ExternalMessageId",
                table: "Correspondence",
                columns: new[] { "Mailbox", "ExternalMessageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Correspondence");
        }
    }
}
