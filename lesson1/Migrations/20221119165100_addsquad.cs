using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace lesson1.Migrations
{
    public partial class addsquad : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AutoGoals",
                table: "PlayerStatistics",
                newName: "OwnGoals");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Matches",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MatchEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Minute = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlayerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    StatisticsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchEvents_MatchStatistics_StatisticsId",
                        column: x => x.StatisticsId,
                        principalTable: "MatchStatistics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchEvents_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchSquad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<int>(type: "int", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    IsCaptain = table.Column<bool>(type: "bit", nullable: false),
                    PlayerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    StatisticsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchSquad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchSquad_MatchStatistics_StatisticsId",
                        column: x => x.StatisticsId,
                        principalTable: "MatchStatistics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchSquad_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_PlayerId",
                table: "MatchEvents",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_StatisticsId",
                table: "MatchEvents",
                column: "StatisticsId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSquad_PlayerId",
                table: "MatchSquad",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSquad_StatisticsId",
                table: "MatchSquad",
                column: "StatisticsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchEvents");

            migrationBuilder.DropTable(
                name: "MatchSquad");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "OwnGoals",
                table: "PlayerStatistics",
                newName: "AutoGoals");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Matches",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
