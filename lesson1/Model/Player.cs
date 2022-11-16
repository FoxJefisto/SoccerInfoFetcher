using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace lesson1.Model
{
    class Player
    {
        public string Id { get; set; }
        public string ImgSource { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Position { get; set; }
        [Column(TypeName = "date")]
        public DateTime? DateOfBirth { get; set; }
        public string WorkingLeg { get; set; }
        public int? Height { get; set; }
        public int? Weight { get; set; }
        public string OriginalName { get; set; }
        public string Citizenship { get; set; }
        public string PlaceOfBirth { get; set; }
        public List<PlayerStatistics> PlayerStatistics { get; set; }
    }
}
