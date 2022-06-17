using Microsoft.EntityFrameworkCore.Migrations;

namespace lesson1.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FootballClubPlayerClub");

            migrationBuilder.DropTable(
                name: "PlayerPlayerClub");

            migrationBuilder.DropTable(
                name: "PlayerClub");

            migrationBuilder.CreateTable(
                name: "FootballClubPlayer",
                columns: table => new
                {
                    ClubsId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlayersId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FootballClubPlayer", x => new { x.ClubsId, x.PlayersId });
                    table.ForeignKey(
                        name: "FK_FootballClubPlayer_Clubs_ClubsId",
                        column: x => x.ClubsId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FootballClubPlayer_Players_PlayersId",
                        column: x => x.PlayersId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FootballClubPlayer_PlayersId",
                table: "FootballClubPlayer",
                column: "PlayersId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FootballClubPlayer");

            migrationBuilder.CreateTable(
                name: "PlayerClub",
                columns: table => new
                {
                    PlayerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClubId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerClub", x => x.PlayerId);
                });

            migrationBuilder.CreateTable(
                name: "FootballClubPlayerClub",
                columns: table => new
                {
                    ClubId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlayerClubPlayerId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FootballClubPlayerClub", x => new { x.ClubId, x.PlayerClubPlayerId });
                    table.ForeignKey(
                        name: "FK_FootballClubPlayerClub_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FootballClubPlayerClub_PlayerClub_PlayerClubPlayerId",
                        column: x => x.PlayerClubPlayerId,
                        principalTable: "PlayerClub",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerPlayerClub",
                columns: table => new
                {
                    PlayerClubPlayerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlayerId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerPlayerClub", x => new { x.PlayerClubPlayerId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_PlayerPlayerClub_PlayerClub_PlayerClubPlayerId",
                        column: x => x.PlayerClubPlayerId,
                        principalTable: "PlayerClub",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerPlayerClub_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FootballClubPlayerClub_PlayerClubPlayerId",
                table: "FootballClubPlayerClub",
                column: "PlayerClubPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPlayerClub_PlayerId",
                table: "PlayerPlayerClub",
                column: "PlayerId");
        }
    }
}
