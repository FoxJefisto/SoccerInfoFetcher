using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace lesson1
{
    class Season
    {
        public int Id { get; set; }

        public string Year { get; set; }

        public string CompetitionId { get; set; }
        [ForeignKey("CompetitionId")]
        public Competition Competition { get; set; }
        public List<PlayerStatistics> PlayerStatistics { get; set; }

        public List<CompetitionTable> Table { get; set; } = new List<CompetitionTable>();
        public List<FootballClubSeason> ClubsSeasons { get; set; } = new List<FootballClubSeason>();
    }
}
