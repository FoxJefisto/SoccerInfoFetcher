using lesson1.Model;
using System.ComponentModel.DataAnnotations.Schema;

namespace lesson1
{
    class PlayerStatistics
    {
        public int Id { get; set; }

        public int SeasonId { get; set; }
        [ForeignKey("SeasonId")]
        public Season Season { get; set; }

        public string PlayerId { get; set; }
        [ForeignKey("PlayerId")]
        public Player PlayerName { get; set; }

        public string Label { get; set; }

        public int? Number { get; set; }

        public string ClubId { get; set; }
        [ForeignKey("ClubId")]
        public FootballClub Club { get; set; }

        public int Goals { get; set; }
        public int Assists { get; set; }
        public int Matches { get; set; }
        public int Minutes { get; set; }
        public int GoalPlusPass { get; set; }
        public int PenGoals { get; set; }
        public int DoubleGoals { get; set; }
        public int HatTricks { get; set; }
        public int AutoGoals { get; set; }
        public int YellowCards { get; set; }
        public int YellowRedCards { get; set; }
        public int RedCards { get; set; }
        public int FairPlayScore { get; set; }
    }
}
