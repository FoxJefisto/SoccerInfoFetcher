using System.Collections.Generic;

namespace lesson1
{
    class Competition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImgSource { get; set; }
        public string Country { get; set; }
        public List<Season> Seasons { get; set; } = new List<Season>();
    }
}
