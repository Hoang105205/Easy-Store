using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddFtsIndexOnNameAndSku : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            CREATE INDEX ""IX_Products_Name_SKU_FTS""
            ON ""Products""
            USING GIN (to_tsvector('simple', coalesce(""Name"", '') || ' ' || coalesce(""SKU"", '')));
        ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX \"IX_Products_Name_SKU_FTS\";");
        }
    }
}
