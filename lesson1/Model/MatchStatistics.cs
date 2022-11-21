using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lesson1.Model
{
    public enum HomeAway : byte
    {
        Home,
        Away
    }

    class MatchStatistics
    {
        public int Id { get; set; }
        public string MatchId { get; set; }
        [ForeignKey("MatchId")]
        public FootballMatch Match { get; set; }
        public string ClubId { get; set; }
        [ForeignKey("ClubId")]
        public FootballClub Club { get; set; }
        public List<MatchEvent> Events { get; set; }
        public List<MatchSquadPlayers> Squad { get; set; }
        public HomeAway HomeAway { get; set; }
        public int? Goals { get; set; }
        public double? Xg { get; set; }
        public int? Shots { get; set; }
        public int? ShotsOnTarget { get; set; }
        public int? ShotsBlocked { get; set; }
        public int? Saves { get; set; }
        public int? BallPossession { get; set; }
        public int? Corners { get; set; }
        public int? Fouls { get; set; }
        public int? Offsides { get; set; }
        public int? YCards { get; set; }
        public int? RCards { get; set; }
        public int? Attacks { get; set; }
        public int? AttacksDangerous { get; set; }
        public int? Passes { get; set; }
        public double? AccPasses { get; set; }
        public int? FreeKicks { get; set; }
        public int? Prowing { get; set; }
        public int? Crosses { get; set; }
        public int? Tackles { get; set; }
    }
}
