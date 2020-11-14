using Microsoft.EntityFrameworkCore.Migrations;

namespace Catalog.API.Infrastructure.CatalogMigrations
{
    public partial class AddTestsField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tests",
                table: "Catalog",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tests",
                table: "Catalog");
        }
    }
}
