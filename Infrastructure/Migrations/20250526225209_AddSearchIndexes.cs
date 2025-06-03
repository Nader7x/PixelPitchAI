using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndexes : Migration
    {        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Match_Teams_Date",
                table: "Matches",
                columns: new[] { "HomeTeamId", "AwayTeamId", "ScheduledDateTimeUtc" })
                .Annotation("Npgsql:IndexMethod", "btree");

            // Add PostgreSQL full-text search indexes using tsvector
              // Teams search indexes
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Teams_Name_Search 
                ON ""Teams"" USING gin(to_tsvector('english', COALESCE(""Name"", '')));");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Teams_League_Search 
                ON ""Teams"" USING gin(to_tsvector('english', COALESCE(""League"", '')));");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Teams_City_Search 
                ON ""Teams"" USING gin(to_tsvector('english', COALESCE(""City"", '')));");

            // Players search indexes
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Players_FullName_Search 
                ON ""Players"" USING gin(to_tsvector('english', COALESCE(""FullName"", '')));");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Players_KnownName_Search 
                ON ""Players"" USING gin(to_tsvector('english', COALESCE(""KnownName"", '')));");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Players_Nationality_Search 
                ON ""Players"" USING gin(to_tsvector('english', COALESCE(""Nationality"", '')));");

            // Coaches search indexes
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Coaches_FirstName_Search 
                ON ""Coaches"" USING gin(to_tsvector('english', COALESCE(""FirstName"", '')));");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Coaches_LastName_Search 
                ON ""Coaches"" USING gin(to_tsvector('english', COALESCE(""LastName"", '')));");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Coaches_FullName_Search 
                ON ""Coaches"" USING gin(to_tsvector('english', COALESCE(""FirstName"" || ' ' || ""LastName"", '')));");
            
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Coaches_Role_Search 
                ON ""Coaches"" USING gin(to_tsvector('english', COALESCE(""Role"", '')));");

            // Stadiums search indexes
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Stadiums_Name_Search 
                ON ""Stadiums"" USING gin(to_tsvector('english', COALESCE(""Name"", '')));");
              migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Stadiums_City_Search 
                ON ""Stadiums"" USING gin(to_tsvector('english', COALESCE(""City"", '')));");
        }/// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Match_Teams_Date",
                table: "Matches");            // Drop PostgreSQL full-text search indexes
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Teams_Name_Search;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Teams_League_Search;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Teams_City_Search;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Players_FullName_Search;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Players_KnownName_Search;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Players_Nationality_Search;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Coaches_FirstName_Search;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Coaches_LastName_Search;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Coaches_FullName_Search;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Coaches_Role_Search;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Stadiums_Name_Search;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Stadiums_City_Search;");
        }
    }
}
