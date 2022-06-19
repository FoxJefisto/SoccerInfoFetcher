using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace lesson1
{
    class Program
    {

        public static RestClient restClient = new RestClient();
        public enum SearchScope
        {
            coaches,
            players,
            clubs,
            games,
            competitions,
            data
        }
        public static FootballClub GetClubInfo(string htmlCode, string clubId)
        {
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);
            var nodeName = doc.DocumentNode.SelectSingleNode(".//div[@class='profile_info new']/h1[@class='profile_info_title red']");
            var nodeEngName = doc.DocumentNode.SelectSingleNode(".//div[@class='profile_info new']/div[@class='profile_en_title']");
            FootballClub club = null;
            string stadium = null;
            string mainCoach = null;
            string name, englishName, fullName, city, country, foundationDate;
            name = englishName = fullName = city = country = foundationDate = null;
            int? rating = null;
            if (nodeName.InnerText.Length != 0)
            {
                name = nodeName.InnerText;
                englishName = nodeEngName.InnerText;
                var rows = doc.DocumentNode.SelectNodes(".//table[@class='profile_params']/tbody/tr");
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        var keyValue = row.SelectNodes(".//td");
                        string key = keyValue[0].InnerText.Trim(),
                               value = keyValue[1].InnerText.Trim();
                        switch (key)
                        {
                            case "Полное название":
                                fullName = value;
                                break;
                            case "Главный тренер":
                                {
                                    Match matchId = Regex.Match(row.InnerHtml, @"coaches/[^>]+>([^<]+)");
                                    mainCoach = matchId.Groups[1].Value;
                                }
                                break;
                            case "Стадион":
                                {
                                    Match matchNameId = Regex.Match(keyValue[1].InnerHtml, @"stadiums/([^/]+)/"">([^<]+)");
                                    stadium = matchNameId.Groups[2].Value;
                                    Match matchCityCountry = Regex.Match(keyValue[1].InnerHtml, @"min_gray"">([^<]+)");
                                    var pair = matchCityCountry.Groups[1].Value.Split(", ");
                                    if (pair.Length == 1)
                                    {
                                        country = pair[0];
                                    }
                                    else
                                    {
                                        city = pair[0];
                                        country = pair[1];
                                    }
                                }
                                break;
                            case "Год основания":
                                foundationDate = value;
                                break;
                            case "Рейтинг УЕФА":
                                rating = int.Parse(value.Split(' ')[0]);
                                break;
                        }
                    }
                }
                club = new FootballClub
                {
                    Id = clubId,
                    Name = name,
                    NameEnglish = englishName,
                    FullName = fullName,
                    MainCoach = mainCoach,
                    Stadium = stadium,
                    City = city,
                    Country = country,
                    FoundationDate = foundationDate,
                    Rating = rating
                };
            }
            return club;
        }

        public static List<string> GetPlayersIdInClub(string clubId)
        {
            string htmlCode = GetHTMLInfo(clubId, SearchScope.clubs, "&tab=squads");
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);
            var nodesClubs = doc.DocumentNode.SelectNodes(".//table[@id='players']/tbody/tr");
            var playersId = new List<string>();
            if (nodesClubs != null)
            {
                foreach (var node in nodesClubs)
                {
                    var player = node.SelectSingleNode(".//td/div[@class='pl_info']/div[@class='tb_pl_club']/a").GetAttributeValue("href", "");
                    Match matchId = Regex.Match(player, "players/([^/]+)");
                    playersId.Add(matchId.Groups[1].Value);
                }
            }
            return playersId;
        }

        public static async Task<List<string>> GetPlayersIdInClubAsync(string clubId)
        {
            return await Task.Run(() => GetPlayersIdInClub(clubId));
        }

        public static Player GetPlayerInfo(string htmlCode, string playerId)
        {
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);
            var nodeName = doc.DocumentNode.SelectSingleNode(".//div[@class='profile_info new']/h1[@class='profile_info_title red']");
            Player player = null;
            string firstName, lastName, fullName, citizenship, placeOfBirth,
                position, workingLeg;
            string clubId = null, nationalTeamId = null;
            firstName = lastName = fullName = citizenship = placeOfBirth = position = workingLeg = null;
            int? numberInClub, numberInNatTeam, height, weight;
            numberInClub = numberInNatTeam = height = weight = null;
            DateTime? dateOfBirth = null;
            if (nodeName.InnerText.Length != 0)
            {
                firstName = nodeName.InnerText.Split(' ')[0];
                lastName = string.Join(' ', nodeName.InnerText.Split(' ').Skip(1).ToArray());
                var rows = doc.DocumentNode.SelectNodes(".//table[@class='profile_params mh200']/tbody/tr");
                foreach (var row in rows)
                {
                    var keyValue = row.SelectNodes(".//td");
                    string key = keyValue[0].InnerText.Trim(),
                           value = keyValue[1].InnerText.Trim();
                    switch (key)
                    {
                        case "Полное имя":
                            fullName = value;
                            break;
                        case "Номер в клубе":
                            numberInClub = int.Parse(value);
                            break;
                        case "Сборная":
                            {
                                Match matchId = Regex.Match(row.OuterHtml, @"clubs/([^/]+)");
                                nationalTeamId = matchId.Groups[1].Value;
                            }
                            break;
                        case "Номер в сборной":
                            if (nationalTeamId == "")
                                continue;
                            numberInNatTeam = int.Parse(value);
                            break;
                        case "Дата рождения":
                            dateOfBirth = DateTime.Parse(value.Split(' ')[0]);
                            break;
                        case "Страна рождения":
                            citizenship = value;
                            break;
                        case "Город рождения":
                            placeOfBirth = value;
                            break;
                        case "Позиция":
                            position = value;
                            break;
                        case "Рабочая нога":
                            workingLeg = value;
                            break;
                        case "Рост/вес":
                            Match matchHW = Regex.Match(value, @"([\d]+)[^\d]*([\d]+)");
                            height = int.TryParse(matchHW.Groups[1].Value, out int heightValue) ? (int?)heightValue : null;
                            weight = int.TryParse(matchHW.Groups[2].Value, out int weightValue) ? (int?)weightValue : null;
                            break;
                    }
                }
                player = new Player
                {
                    Id = playerId,
                    FirstName = firstName,
                    LastName = lastName,
                    Number = numberInClub,
                    Position = position,
                    DateOfBirth = dateOfBirth,
                    WorkingLeg = workingLeg,
                    Height = height,
                    Weight = weight,
                    OriginalName = fullName,
                    Citizenship = citizenship,
                    PlaceOfBirth = placeOfBirth
                };
            }
            return player;
        }

        public static Player GetPlayerInfoById(string playerId)
        {
            string htmlCode = GetHTMLInfo(playerId, SearchScope.players);
            return GetPlayerInfo(htmlCode, playerId);
        }
        public static FootballClub GetClubInfoById(string clubId)
        {
            string htmlCode = GetHTMLInfo(clubId, SearchScope.clubs);
            return GetClubInfo(htmlCode, clubId);
        }
        public static async Task<Player> GetPlayerInfoByIdAsync(string playerId)
        {
            return await Task.Run(() => GetPlayerInfoById(playerId));
        }
        public static async Task<FootballClub> GetClubInfoByIdAsync(string clubId)
        {
            return await Task.Run(() => GetClubInfoById(clubId));
        }
        public static string GetHTMLInfo(string id, SearchScope scope, string data1 = null, string data2 = null)
        {
            string address;
            if (scope == SearchScope.data)
            {
                address = $"https://soccer365.ru/";
            }
            else
            {
                address = $"https://soccer365.ru/{scope}/{id}/";
            }

            if (data1 != null)
                address += data1;
            if (data2 != null)
                address += data2;

            return restClient.GetStringAsync(address).Result;
        }
        public static List<string> GetClubsIdInLeague(string value)
        {
            string htmlCode = GetHTMLInfo(value, SearchScope.competitions, "teams/");
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);
            var nodesClubs = doc.DocumentNode.SelectNodes(".//table[@id='coaches']/tbody/tr");
            List<string> clubs = new List<string>();
            if (nodesClubs != null)
            {
                foreach (var node in nodesClubs)
                {
                    var club = node.SelectSingleNode(".//div[@class='img16']/span/a").GetAttributeValue("href", "");
                    Match matchId = Regex.Match(club, "clubs/([^/]+)");
                    clubs.Add(matchId.Groups[1].Value);
                }
            }
            return clubs;
        }

        public static (Competition comp, List<string> seasonsYear) GetCompetitionInfo(string compId)
        {
            string htmlCode = GetHTMLInfo(compId, SearchScope.competitions);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);
            string compName = doc.DocumentNode.SelectSingleNode(".//h1[@class='profile_info_title red']").InnerText;
            var nodesInfo = doc.DocumentNode.SelectNodes(".//table[@class='profile_params']/tbody/tr");
            string compCountry = null;
            foreach (var nodeInfo in nodesInfo)
            {
                if (nodeInfo.SelectSingleNode(".//td").InnerText == "Страна")
                {
                    compCountry = nodeInfo.SelectSingleNode(".//span").InnerText;
                }
            }
            var comp = new Competition
            {
                Id = compId,
                Name = compName,
                Country = compCountry
            };
            var nodesSelectbox = doc.DocumentNode.SelectSingleNode(".//div[@class='breadcrumb']").SelectNodes(".//div[@class='selectbox-menu']/a");
            var seasonsYear = new List<string>();
            foreach (var nodeSelectBox in nodesSelectbox)
            {
                Match matchSeason = Regex.Match(nodeSelectBox.InnerText, @"^[\d/]+$");
                if (matchSeason.Success)
                {
                        seasonsYear.Add(nodeSelectBox.InnerText.Replace('/', '-'));
                }
            }
            return (comp, seasonsYear);
        }

        public static PlayerStatistics GetCompPlayerStatisticsById(string compId, string season = "")
        {
            string htmlCode = GetHTMLInfo(compId, SearchScope.competitions, $"{season}/players/");
            if (season == "")
                season = Regex.Match(htmlCode, @"selectbox-label"">([^<]+)").Groups[1].Value;
            string cp_ss = Regex.Match(htmlCode, @"cp_ss=([\d]+)").Groups[1].Value;
            htmlCode = GetHTMLInfo("", SearchScope.data, $"?c=competitions&a=tab_tablesorter_players&cp_ss={cp_ss}&cl=0&page=0&size=0&col[1]=1&col[4]=0");
            var json = JObject.Parse(htmlCode);
            var playersStats = new List<RowinPlayerStatistics>();
            var doc = new HtmlAgilityPack.HtmlDocument();
            foreach (var row in json["rows"])
            {
                string playerId = null, clubId = null;
                string htmlNameInfo = row[0].ToString();
                doc.LoadHtml(htmlNameInfo);
                var hrefCell = doc.DocumentNode.SelectSingleNode(".//a[@class='name']");
                if (hrefCell != null)
                {
                    string hrefStr = hrefCell.GetAttributeValue("href", "");
                    Match matchPlayerId = Regex.Match(hrefStr, @".*/(\d+)/");
                    if (matchPlayerId.Groups[1].Success)
                        playerId = matchPlayerId.Groups[1].Value;
                }
                Match matchClubId = Regex.Match(htmlNameInfo, @"clubs/([\d]+)");
                if (matchClubId.Groups[1].Success)
                    clubId = matchClubId.Groups[1].Value;
                var stats = Array.ConvertAll(row.Skip(1).ToArray(), x => { if (x.ToString() != "&nbsp;") return (int)x; else return 0; });
                var playerStats = new RowinPlayerStatistics
                {
                    PlayerId = playerId,
                    Goals = stats[0],
                    Assists = stats[1],
                    Matches = stats[2],
                    Minutes = stats[3],
                    GoalPlusPass = stats[4],
                    PenGoals = stats[5],
                    DoubleGoals = stats[6],
                    HatTricks = stats[7],
                    AutoGoals = stats[8],
                    YellowCards = stats[9],
                    YellowRedCards = stats[10],
                    RedCards = stats[11],
                    FairPlayScore = stats[12]
                };
                playersStats.Add(playerStats);
            }
            var competitionPlayerStatistics = new PlayerStatistics
            {
                Rows = playersStats
            };
            return competitionPlayerStatistics;
        }

        //Асинхронный
        private static async Task SaveCompsAsync(List<string> compsId)
        {
            var clubsId = new List<string>();
            foreach (var compId in compsId)
            {
                clubsId.AddRange(GetClubsIdInLeague(compId));
            }
            clubsId.Distinct();
            foreach (var clubId in clubsId)
            {
                await SaveClubsAsync(clubId);
            }
        }

        private static async Task SaveClubsAsync(string clubId)
        {
            bool notExist = false;

            using (AppContext db = new AppContext())
            {
                if (!db.Clubs.Any(x => x.Id == clubId))
                {
                    var clubInfo = GetClubInfoById(clubId);
                    db.Clubs.Add(clubInfo);
                    db.SaveChanges();
                    notExist = true;
                }
                Console.WriteLine($"Клубов загружено: {db.Clubs.Count()}");
                Console.WriteLine($"Игроков загружено: {db.Players.Count()}");
            }
            if (notExist)
            {
                var playersId = GetPlayersIdInClub(clubId);
                foreach (var playerId in playersId)
                {
                    await SavePlayerAsync(playerId, clubId);
                }
            }
        }

        private static async Task SavePlayerAsync(string playerId, string clubId)
        {

            using (AppContext db = new AppContext())
            {
                if (!db.Players.Any(x => x.Id == playerId))
                {
                    var playerInfo = await GetPlayerInfoByIdAsync(playerId);
                    db.Players.Add(playerInfo);
                }
                FootballClubPlayer clubPlayer = new FootballClubPlayer { ClubId = clubId, PlayerId = playerId };
                if (!db.ClubsPlayers.Contains(clubPlayer))
                {
                    db.ClubsPlayers.Add(clubPlayer);
                }
                db.SaveChanges();
            }
        }

        public static List<string> GetMainCompsId(int page = 1)
        {
            var compsId = new List<string>();
            if (page == 2) return compsId;
            string htmlCode = GetHTMLInfo(null, SearchScope.data, "index.php?c=competitions&a=champs_list_data&tp=0&cn_id=0&st=0&ttl=&p=", page.ToString());
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);
            var nodesSeasonItems = doc.DocumentNode.SelectNodes(".//div[@class='season_items']/div[@class='season_item']");
            foreach (var nodeSeasonItem in nodesSeasonItems)
            {
                string competition = nodeSeasonItem.SelectSingleNode(".//div[@class='block_body']/a").GetAttributeValue("href", "");
                Match matchId = Regex.Match(competition, "competitions/([^/]+)");
                compsId.Add(matchId.Groups[1].Value);
            }
            compsId.AddRange(GetMainCompsId(page + 1));
            return compsId;
        }


        static async Task Main(string[] args)
        {
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            //var compsId = GetMainCompsId();
            ////var compsId = new List<string> { "12" };
            //await SaveCompsAsync(compsId);
            //stopwatch.Stop();
            //TimeSpan ts = stopwatch.Elapsed;
            //string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //    ts.Hours, ts.Minutes, ts.Seconds,
            //    ts.Milliseconds / 10);
            //Console.WriteLine($"Время работы: { elapsedTime}");
            var compsId = GetMainCompsId();
            foreach (var compId in compsId)
            {
                using (AppContext db = new AppContext())
                {
                    var tuple = GetCompetitionInfo(compId);
                    if (!db.Competitions.Any(x => x.Id == compId))
                    {
                        db.Competitions.Add(tuple.comp);
                    }
                    db.SaveChanges();
                    foreach (var seasonYear in tuple.seasonsYear)
                    {
                        if(!db.Seasons.Any(x => x.CompetitionId == compId && x.Year == seasonYear))
                        {
                            Season season = new Season
                            {
                                Year = seasonYear,
                                CompetitionId = compId
                            };
                            db.Seasons.Add(season);
                        }
                    }
                    db.SaveChanges();
                    Console.WriteLine($"Добавлено лиг: {db.Competitions.Count()}");
                    Console.WriteLine($"Добавлено сезонов: {db.Seasons.Count()}");
                }
            }

        }
    }

    class Competition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public List<Season> Seasons { get; set; } = new List<Season>();
    }

    class Season
    {
        public int Id { get; set; }

        public string Year { get; set; }
        public string CompetitionId { get; set; }
        [ForeignKey("CompetitionId")]
        public Competition Competition { get; set; }

        //public List<CompetitionTable> Table { get; set; } = new List<CompetitionTable>();
        //public List<FootballClubSeason> ClubCompetitionSeasons { get; set; } = new List<FootballClubSeason>();
    }
    [NotMapped]
    class PlayerStatistics
    {
        public string Id { get; set; }
        public string CompetitionSeasonId { get; set; }
        [ForeignKey("CompetitionSeasonId")]
        public Season CompetitionSeason { get; set; }
        public List<RowinPlayerStatistics> Rows { get; set; } = new List<RowinPlayerStatistics>();
    }
    [NotMapped]
    class RowinPlayerStatistics
    {
        public string Id { get; set; }
        public string PlayerStatisticsId { get; set; }
        [ForeignKey("PlayerStatisticsId")]
        public PlayerStatistics PlayerStatistics { get; set; }
        public string PlayerId { get; set; }
        [ForeignKey("PlayerId")]
        public Player PlayerName { get; set; }
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
    [NotMapped]
    class CompetitionTable
    {
        public string Id { get; set; }
        public string GroupName { get; set; }
        public string CompetitionSeasonId { get; set; }
        [ForeignKey("CompetitionSeasonId")]
        public Season CompetitionSeason { get; set; }
        public List<RowInCompetitionTable> Rows { get; set; } = new List<RowInCompetitionTable>();
    }
    [NotMapped]
    class RowInCompetitionTable
    {
        public string Id { get; set; }
        public string CompetitionTableId { get; set; }
        [ForeignKey("CompetitionTableId")]
        public CompetitionTable CompetitionTable { get; set; }
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

    class FootballClub
    {
        public string Id { get; set; }
        [Column("ClubName")]
        public string Name { get; set; }
        public string NameEnglish { get; set; }
        public string FullName { get; set; }
        public string MainCoach { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Stadium { get; set; }
        public string FoundationDate { get; set; }
        public int? Rating { get; set; }
        public List<FootballClubPlayer> ClubPlayer { get; set; } = new List<FootballClubPlayer>();

        [NotMapped]
        public List<FootballClubSeason> ClubCompetitionSeasons { get; set; } = new List<FootballClubSeason>();
    }
    [NotMapped]
    class FootballClubSeason
    {
        public string ClubId { get; set; }
        //[ForeignKey("ClubId")]
        public FootballClub Club { get; set; }
        public string CompetitionSeasonId { get; set; }
        //[ForeignKey("CompetitionSeasonId")]
        public Season CompetitionSeason { get; set; }
    }

    class Player
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Number { get; set; }
        public string Position { get; set; }
        [Column(TypeName = "date")]
        public DateTime? DateOfBirth { get; set; }
        public string WorkingLeg { get; set; }
        public int? Height { get; set; }
        public int? Weight { get; set; }
        public string OriginalName { get; set; }
        public string Citizenship { get; set; }
        public string PlaceOfBirth { get; set; }
        public List<FootballClubPlayer> ClubPlayer { get; set; } = new List<FootballClubPlayer>();
    }

    class FootballClubPlayer
    {
        public string ClubId { get; set; }
        public FootballClub Club { get; set; }
        public string PlayerId { get; set; }
        public Player Player { get; set; }

    }

    class AppContext : DbContext
    {
        public DbSet<Competition> Competitions { get; set; }
        public DbSet<Season> Seasons { get; set; }
        //public DbSet<PlayerStatistics> PlayerStatistics { get; set; }
        //public DbSet<RowinPlayerStatistics> RowsinPlayerStatistics { get; set; }
        //public DbSet<CompetitionTable> CompetitionTable { get; set; }
        //public DbSet<RowInCompetitionTable> RowsInCompetitionTable { get; set; }
        //public DbSet<FootballClubSeason> ClubsSeasons { get; set; }
        public DbSet<FootballClub> Clubs { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<FootballClubPlayer> ClubsPlayers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS; DATABASE=FootballDB; Trusted_Connection=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FootballClubPlayer>()
                .HasKey(t => new { t.ClubId, t.PlayerId });
            modelBuilder.Entity<FootballClubPlayer>()
                .HasOne(p => p.Player).WithMany(p => p.ClubPlayer).HasForeignKey(p => p.PlayerId);
            modelBuilder.Entity<FootballClubPlayer>()
                .HasOne(c => c.Club).WithMany(c => c.ClubPlayer).HasForeignKey(c => c.ClubId);

            //modelBuilder.Entity<FootballClubSeason>()
            //    .HasKey(t => new { t.ClubId, t.CompetitionSeasonId });
            //modelBuilder.Entity<FootballClubSeason>()
            //    .HasOne(c => c.Club).WithMany(c => c.ClubCompetitionSeasons).HasForeignKey(c => c.ClubId);
            //modelBuilder.Entity<FootballClubSeason>()
            //    .HasOne(s => s.CompetitionSeason).WithMany(s => s.ClubCompetitionSeasons).HasForeignKey(s => s.CompetitionSeasonId);

        }
    }
}
