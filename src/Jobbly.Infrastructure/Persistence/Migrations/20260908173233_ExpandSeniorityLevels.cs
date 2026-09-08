using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobbly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandSeniorityLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backstop for the 12-value SeniorityLevel enum: rows enriched under
            // the old enum carry 'Staff' (member removed) and would throw on
            // read (string -> enum conversion). Map them conservatively to
            // 'Senior' so reads work; the pipeline re-run then re-enriches
            // every listed job to its true new level. 'Mid' -> 'MidLevel' is
            // the exact semantic match.
            migrationBuilder.Sql(
                """
                UPDATE jobs SET "SeniorityLevel" = 'Senior' WHERE "SeniorityLevel" = 'Staff';
                UPDATE jobs SET "SeniorityLevel" = 'MidLevel' WHERE "SeniorityLevel" = 'Mid';
                UPDATE user_profiles SET "SeniorityLevel" = 'Senior' WHERE "SeniorityLevel" = 'Staff';
                UPDATE user_profiles SET "SeniorityLevel" = 'MidLevel' WHERE "SeniorityLevel" = 'Mid';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible by design: re-enriched rows can't be mapped back to
            // the old 6-value enum. Rollback requires restoring a backup.
        }
    }
}
