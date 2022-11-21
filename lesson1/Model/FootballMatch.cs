using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lesson1.Model
{
    class FootballMatch
    {
        public string Id { get; set; }
        public string? Label { get; set; }
        public string? Stage { get; set; }
        public DateTime? Date { get; set; }

        public string? Status { get; set; }
        public int SeasonId { get; set; }
        [ForeignKey("SeasonId")]
        public Season Season { get; set; }
        public List<MatchStatistics> Statistics { get; set; }
    }
}
