using Microsoft.EntityFrameworkCore.Migrations;

namespace lesson1.Migrations
{
    public partial class second : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStatistics_Seasons_PlayerStatisticsId",
                table: "PlayerStatistics");

            migrationBuilder.DropIndex(
                name: "IX_PlayerStatistics_PlayerStatisticsId",
                table: "PlayerStatistics");

            migrationBuilder.DropColumn(
                name: "PlayerStatisticsId",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "PlayerStatisticsId",
                table: "PlayerStatistics");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlayerStatisticsId",
                table: "Seasons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlayerStatisticsId",
                table: "PlayerStatistics",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStatistics_PlayerStatisticsId",
                table: "PlayerStatistics",
                column: "PlayerStatisticsId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStatistics_Seasons_PlayerStatisticsId",
                table: "PlayerStatistics",
                column: "PlayerStatisticsId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
