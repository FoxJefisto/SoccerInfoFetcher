using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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
        private enum SearchScope
        {
            coaches,
            players,
            clubs,
            games,
            competitions,
            data
        }
        private static FootballClub GetClubInfo(string htmlCode, string clubId)
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
                club = new FootballClub { Id = clubId, Name = name, NameEnglish = englishName, FullName = fullName, MainCoach = mainCoach, 
                    Stadium = stadium, City = city, Country = country, FoundationDate = foundationDate, Rating = rating };
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
                        case "Клуб":
                            {
                                Match matchId = Regex.Match(row.OuterHtml, @"clubs/([^/]+)");
                                clubId = matchId.Groups[1].Value;
                            }
                            break;
                        case "В аренде":
                            {
                                Match matchId = Regex.Match(row.OuterHtml, @"clubs/([^/]+)/"">([^<]+)");
                                clubId = matchId.Groups[1].Value;
                            }
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
                player = new Player {
                Id = playerId,
                FirstName = firstName,
                LastName = lastName,
                ClubId = clubId,
                Number = numberInClub,
                Position = position,
                DateOfBirth = dateOfBirth,
                WorkingLeg = workingLeg,
                Height = height,
                Weight = weight,
                OriginalName = fullName,
                Citizenship = citizenship,
                PlaceOfBirth = placeOfBirth};
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
        private static string GetHTMLInfo(string id, SearchScope scope, string data1 = null, string data2 = null)
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
        private static List<string> GetClubsIdInLeague(string value)
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

        public static IEnumerable<List<T>> SplitIntoSets<T>(IEnumerable<T> source, int itemsPerSet)
        {
            var sourceList = source as List<T> ?? source.ToList();
            for (var index = 0; index < sourceList.Count; index += itemsPerSet)
            {
                yield return sourceList.Skip(index).Take(itemsPerSet).ToList();
            }
        }

        //Асинхронный
        private static List<FootballClub> FindPartOfClubs(List<string> clubsId, string compId, int taskId, int subTaskId)
        {
            var clubsList = new List<FootballClub>();
            foreach (var clubId in clubsId)
            {
                Console.WriteLine($"{new string(' ', taskId * 10)}{compId}{Convert.ToChar(subTaskId + 97)}");
                var club = GetClubInfoById(clubId);
                clubsList.Add(club);
            }
            return clubsList;
        }
        //Асинхронный
        private static List<FootballClub> FindClubs(List<string> compsId, int taskId)
        {
            var clubsList = new List<FootballClub>();
            foreach (var compId in compsId)
            {

                var clubsId = GetClubsIdInLeague(compId);
                int LEN = 10;
                var subClubs = SplitIntoSets(clubsId, LEN).ToList();
                Task<List<FootballClub>>[] tasks = new Task<List<FootballClub>>[subClubs.Count()];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = new Task<List<FootballClub>>(() => FindPartOfClubs(subClubs[i], compId, taskId, i));
                    tasks[i].Start();
                    Thread.Sleep(1500);
                }
                Task.WaitAll(tasks);
                for (int i = 0; i < tasks.Length; i++)
                {
                    clubsList.AddRange(tasks[i].Result);
                }
            }
            return clubsList;
        }
        //Асинхронный
        private static List<Player> FindPartOfPlayers(List<string> playersId, string clubId, int taskId, int subTaskId)
        {
            var playerList = new List<Player>();
            foreach (var playerId in playersId)
            {
                Console.WriteLine($"{new string(' ', taskId * 10)}{clubId}{Convert.ToChar(subTaskId + 97)}");
                var player = GetPlayerInfoById(playerId);
                playerList.Add(player);
            }
            return playerList;
        }
        //Асинхронный
        private static List<Player> FindPlayers(List<string> clubsId, int taskId)
        {
            var playersList = new List<Player>();
            foreach (var clubId in clubsId)
            {
                var playersId = GetPlayersIdInClub(clubId);
                int LEN = 10;
                var subPlayers = SplitIntoSets(playersId, LEN).ToList();
                Task<List<Player>>[] tasks = new Task<List<Player>>[subPlayers.Count()];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = new Task<List<Player>>(() => FindPartOfPlayers(subPlayers[i], clubId, taskId, i));
                    tasks[i].Start();
                    Thread.Sleep(1500);
                }
                Task.WaitAll(tasks);
                for (int i = 0; i < tasks.Length; i++)
                {
                    playersList.AddRange(tasks[i].Result);
                }
            }
            return playersList;
        }

        public static List<string> GetMainCompsId(int page = 1)
        {
            var compsId = new List<string>();
            if (page == 6) return compsId;
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



        private static void SaveClubsInBD(List<string> compsId)
        {
            var clubsList = new List<FootballClub>();
            int LEN = 10;
            var subComps = SplitIntoSets(compsId, LEN).ToList();
            Task<List<FootballClub>>[] tasks = new Task<List<FootballClub>>[subComps.Count()];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = new Task<List<FootballClub>>(() => FindClubs(subComps[i], i));
                tasks[i].Start();
                Thread.Sleep(1500);
            }
            Task.WaitAll(tasks);
            Console.WriteLine("Обработка результатов");
            for (int i = 0; i < tasks.Length; i++)
            {
                clubsList.AddRange(tasks[i].Result);
            }
            Console.WriteLine("Фильтрация данных");
            var clubs = clubsList.GroupBy(x => x.Id).Select(g => g.First());
            using (AppContext db = new AppContext())
            {
                db.Clubs.AddRange(clubs);
                db.SaveChanges();
            }
        }



        private static void SavePlayersInBD(List<string> compsId)
        {
            var clubsId = new List<string>();
            foreach (var compId in compsId)
            {
                clubsId.AddRange(GetClubsIdInLeague(compId));
            }
            var playersList = new List<Player>();
            int LEN = 10;
            var subClubs = SplitIntoSets(clubsId, LEN).ToList();
            var tasks = new Task<List<Player>>[subClubs.Count()];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = new Task<List<Player>>(() => FindPlayers(subClubs[i], i));
                tasks[i].Start();
                Thread.Sleep(1500);
            }
            Task.WaitAll(tasks);
            Console.WriteLine("Обработка результатов");
            for (int i = 0; i < tasks.Length; i++)
            {
                playersList.AddRange(tasks[i].Result);
            }
            Console.WriteLine("Фильтрация данных");
            var players = playersList.GroupBy(x => x.Id).Select(g => g.First());
            using (AppContext db = new AppContext())
            {
                db.Players.AddRange(players);
                db.SaveChanges();
            }
        }


        static void Main(string[] args)
        {
            //var comps = GetMainCompsId();
            var comps = new List<string> { "12","13" };
            SaveClubsInBD(comps);
            SavePlayersInBD(comps);
        }
    }

    class FootballClub
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string NameEnglish { get; set; }
        public string FullName { get; set; }
        public string MainCoach { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Stadium { get; set; }
        public string FoundationDate { get; set; }
        public int? Rating { get; set; }
        public List<Player> Players { get; set; }
    }

    class Player
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ClubId { get; set; }
        [ForeignKey("ClubId")]
        public FootballClub Club { get; set; }
        public int? Number { get; set; }
        public string Position { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string WorkingLeg { get; set; }
        public int? Height { get; set; }
        public int? Weight { get; set; }
        public string OriginalName { get; set; }
        public string Citizenship { get; set; }
        public string PlaceOfBirth { get; set; }
    }

    class AppContext : DbContext
    {
        public DbSet<FootballClub> Clubs { get; set; }
        public DbSet<Player> Players { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS; DATABASE=TestDB; Trusted_Connection=True");
        }
    }
}
