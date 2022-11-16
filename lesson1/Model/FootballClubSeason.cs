using System.ComponentModel.DataAnnotations.Schema;

namespace lesson1
{
    class FootballClubSeason
    {
        public string ClubId { get; set; }
        [ForeignKey("ClubId")]
        public FootballClub Club { get; set; }
        public int SeasonId { get; set; }
        [ForeignKey("SeasonId")]
        public Season Season { get; set; }
    }
}
