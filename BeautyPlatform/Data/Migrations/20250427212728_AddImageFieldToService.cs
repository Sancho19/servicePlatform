using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeautyPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImageFieldToService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Services",
                type: "nvarchar(max)",
                nullable: true); // or nullable: false with defaultValue: "" if you want
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Services");
        }
    }
}
