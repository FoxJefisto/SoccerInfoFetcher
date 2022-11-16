using System.ComponentModel.DataAnnotations.Schema;

namespace lesson1
{
    class CompetitionTable
    {
        public int Id { get; set; }

        public string GroupName { get; set; }

        public int SeasonId { get; set; }
        [ForeignKey("SeasonId")]
        public Season Season { get; set; }

        public int Position { get; set; }

        public string ClubId { get; set; }

        [ForeignKey("ClubId")]
        public FootballClub Club { get; set; }

        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int GamesDrawn { get; set; }
        public int GamesLost { get; set; }
        public int GoalsScored { get; set; }
        public int GoalsMissed { get; set; }
        public int GoalsDifference { get; set; }
        public int Points { get; set; }
    }
}
