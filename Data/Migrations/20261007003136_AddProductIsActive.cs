using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace app_curso_claude.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Products");
        }
    }
}
