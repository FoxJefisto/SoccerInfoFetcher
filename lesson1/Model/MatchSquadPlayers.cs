using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lesson1.Model
{
    public enum SquadType : byte
    {
        Lineup,
        Probably,
        Substitute,
        Unknown
    }
    class MatchSquadPlayers
    {
        public int Id { get; set; }
        public int? Number { get; set; }
        public string Label { get; set; }
        public SquadType Type { get; set; }
        public bool IsCaptain { get; set; }
        public string PlayerId { get; set; }
        [ForeignKey("PlayerId")]
        public Player Player { get; set; }
        public int StatisticsId { get; set; }
        public MatchStatistics Statistics { get; set; }
    }
}
