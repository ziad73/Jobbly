using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobbly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDedupFingerprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DedupFingerprint",
                table: "jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_DedupFingerprint",
                table: "jobs",
                column: "DedupFingerprint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_jobs_DedupFingerprint",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "DedupFingerprint",
                table: "jobs");
        }
    }
}
