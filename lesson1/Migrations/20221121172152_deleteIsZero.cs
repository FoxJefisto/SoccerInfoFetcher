using Microsoft.EntityFrameworkCore.Migrations;

namespace lesson1.Migrations
{
    public partial class deleteIsZero : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsZero",
                table: "PlayerStatistics");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsZero",
                table: "PlayerStatistics",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
