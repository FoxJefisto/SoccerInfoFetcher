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
using System.Runtime.InteropServices.ComTypes;
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

        /// <summary>
        /// Получает и возвращает информацию о клубе. Осуществляет парсинг по html строке
        /// </summary>
        /// <param name="htmlCode"></param>
        /// <param name="clubId"></param>
        /// <returns></returns>
        public static FootballClub GetClubInfo(string htmlCode, string clubId)
        {
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);
            var nodeName = doc.DocumentNode.SelectSingleNode(".//div[@class='profile_info new']/h1[@class='profile_info_title red']");
            if (nodeName == null)
            {
                return null;
            }
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

        /// <summary>
        /// Возвращает информацию о клубе с сайта
        /// </summary>
        /// <param name="clubId"></param>
        /// <returns></returns>
        public static FootballClub GetClubInfoById(string clubId)
        {
            string htmlCode = GetHTMLInfo(clubId, SearchScope.clubs);
            return GetClubInfo(htmlCode, clubId);
        }

        /// <summary>
        /// Возвращает id игроков, которые играют в клубе с данным clubId
        /// </summary>
        /// <param name="clubId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Возвращает экземпляр класса Player и Id его клуба. Осуществляет парсинг по html строке
        /// </summary>
        /// <param name="htmlCode"></param>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public static (Player player, string clubId) GetPlayerInfo(string htmlCode, string playerId)
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
                if (rows != null)
                {
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
                                    Match matchId = Regex.Match(row.InnerHtml, @"clubs/([^/]+)");
                                    clubId = matchId.Groups[1].Value;
                                }
                                break;
                            case "В аренде":
                                {
                                    Match matchId = Regex.Match(row.InnerHtml, @"clubs/([^/]+)");
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
            return (player, clubId);
        }

        /// <summary>
        /// Возвращает экземпляр класса Player и Id его клуба
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public static (Player player, string clubId) GetPlayerInfoById(string playerId)
        {
            string htmlCode = GetHTMLInfo(playerId, SearchScope.players);
            return GetPlayerInfo(htmlCode, playerId);
        }

        /// <summary>
        /// Возвращает строку содержащую html код страницы.
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="scope">Тип сущности для id</param>
        /// <param name="data1">Дополнительная строка для дополнения ссылки</param>
        /// <param name="data2">Дополнительная строка для дополнения ссылки</param>
        /// <returns></returns>
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

        /// <summary>
        /// Получить клубы определенного сезона
        /// </summary>
        /// <param name="compId">id Соревнования</param>
        /// <param name="season">Год сезона</param>
        /// <returns></returns>
        public static List<string> GetClubsIdInLeague(string compId, string season)
        {
            string htmlCode = GetHTMLInfo(compId, SearchScope.competitions, $"{season}/teams/");
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

        /// <summary>
        /// Возвращает информацию о клубе по его id
        /// </summary>
        /// <param name="compId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Возвращает все id игроков и все id клубов из сезона
        /// </summary>
        /// <param name="compId">id соревнования</param>
        /// <param name="season">год сезона</param>
        /// <returns></returns>
        public static (List<string> playersId, List<string> clubsId) GetAllPlayersAndClubsIdInSeason(string compId, string season = "")
        {
            string htmlCode = GetHTMLInfo(compId, SearchScope.competitions, $"{season}/players/");
            if (season == "")
                season = Regex.Match(htmlCode, @"selectbox-label"">([^<]+)").Groups[1].Value;
            string cp_ss = Regex.Match(htmlCode, @"cp_ss=([\d]+)").Groups[1].Value;
            htmlCode = GetHTMLInfo("", SearchScope.data, $"?c=competitions&a=tab_tablesorter_players&cp_ss={cp_ss}&cl=0&page=0&size=0&col[1]=1&col[4]=0");
            var json = JObject.Parse(htmlCode);
            var playersId = new List<string>();
            var clubsId = new List<string>();
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
                playersId.Add(playerId);
                if (!clubsId.Any((x) => x == clubId))
                {
                    clubsId.Add(clubId);
                }
            }
            return (playersId, clubsId);
        }

        /// <summary>
        /// Возвращает статистику игроков в заданном соревновании и заданном сезоне
        /// </summary>
        /// <param name="compId">id соревнования</param>
        /// <param name="seasonId">id сезона</param>
        /// <param name="season">год сезона</param>
        /// <returns></returns>
        public static List<PlayerStatistics> GetSeasonPlayerStatisticsById(string compId, int seasonId, string season = "")
        {
            string htmlCode = GetHTMLInfo(compId, SearchScope.competitions, $"{season}/players/");
            if (season == "")
                season = Regex.Match(htmlCode, @"selectbox-label"">([^<]+)").Groups[1].Value;
            string cp_ss = Regex.Match(htmlCode, @"cp_ss=([\d]+)").Groups[1].Value;
            htmlCode = GetHTMLInfo("", SearchScope.data, $"?c=competitions&a=tab_tablesorter_players&cp_ss={cp_ss}&cl=0&page=0&size=0&col[1]=1&col[4]=0");
            var json = JObject.Parse(htmlCode);
            var playersStats = new List<PlayerStatistics>();
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
                var playerStats = new PlayerStatistics
                {
                    PlayerId = playerId,
                    ClubId = clubId,
                    SeasonId = seasonId,
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
            return playersStats;
        }

        public static List<CompetitionTable> GetCompetitionTableById(string id, int seasonId, string season)
        {
            string htmlCode = GetHTMLInfo(id, SearchScope.competitions, season + "/");
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);
            var nodesTable = doc.DocumentNode.SelectNodes(".//table[@class='stngs']");
            if (nodesTable == null)
                return null;
            List<CompetitionTable> table = new List<CompetitionTable>();
            int iTable = 0;
            foreach (var nodeTable in nodesTable)
            {
                var nodeRows = nodeTable.SelectNodes(".//tbody/tr");
                if (nodeRows.Count == 0)
                    return null;
                string groupName = null;
                if (nodesTable.Count > 1)
                {
                    groupName = Convert.ToString(Convert.ToChar(65 + iTable));
                }
                foreach (var nodeRow in nodeRows)
                {
                    var nodePosition = nodeRow.SelectSingleNode(".//div[@class]");
                    int position = int.Parse(nodePosition.InnerText);
                    var nodeName = nodeRow.SelectSingleNode(".//span");
                    string clubId = null;
                    Match matchClub = Regex.Match(nodeName.InnerHtml, @"href=""\/clubs\/([0-9]+)");
                    if (matchClub.Success)
                    {
                        clubId = matchClub.Groups[1].Value;
                    }
                    int[] sts = new int[8];
                    int jColumn = 0;
                    var nodeColumns = nodeRow.SelectNodes(".//td[@class='ctr']");
                    foreach (var nodeColumn in nodeColumns)
                    {
                        sts[jColumn] = int.Parse(nodeColumn.InnerText);
                        jColumn++;
                    }
                    CompetitionTable row = new CompetitionTable
                    {
                        Position = position,
                        ClubId = clubId,
                        GamesPlayed = sts[0],
                        GamesWon = sts[1],
                        GamesDrawn = sts[2],
                        GamesLost = sts[3],
                        GoalsScored = sts[4],
                        GoalsMissed = sts[5],
                        GoalsDifference = sts[6],
                        Points = sts[7],
                        GroupName = groupName,
                        SeasonId = seasonId
                    };
                    table.Add(row);
                }
                iTable++;
            }
            return table;
        }

        public static void SaveDB(List<string> compsId)
        {
            //SaveCompetitionsInfoAsync(compsId).GetAwaiter().GetResult();
            Season[] seasons;
            using (AppContext db = new AppContext())
            {
                seasons = db.Seasons.ToArray();
            }
            //var clubsId = new List<string>();
            //var playersId = new List<string>();
            //foreach (var season in seasons)
            //{
            //    var result = GetAllPlayersAndClubsIdInSeason(season.CompetitionId, season.Year);
            //    playersId.AddRange(result.Item1);
            //    clubsId.AddRange(result.Item2);
            //}
            //var players = playersId.Distinct().ToArray();
            //var clubs = clubsId.Distinct().ToArray();
            //Console.WriteLine($"Клубов получено: {clubs.Length}");
            //Console.WriteLine($"Игроков получено: {players.Length}");

            //SaveClubsAsync(clubs.Where(x => x != null).ToArray()).GetAwaiter().GetResult();
            //SavePlayersAsync(players.Where(x => x != null).ToArray()).GetAwaiter().GetResult();
            //SaveSeasonsClubsIdAsync(seasons).GetAwaiter().GetResult();
            SaveSeasonsPlayerStatisticsAsync(seasons).GetAwaiter().GetResult();
            SaveSeasonsCompetitionTableAsync(seasons).GetAwaiter().GetResult();
        }

        public static async Task SaveCompetitionsInfoAsync(List<string> compsId)
        {
            Task[] tasks = new Task[compsId.Count];
            int i = 0;
            foreach (var compId in compsId)
            {
                tasks[i] = SaveOneCompetitionInfoAsync(compId);
                i++;
            }
            await Task.WhenAll(tasks);
        }

        public static async Task SaveOneCompetitionInfoAsync(string compId)
        {
            await Task.Run(() => SaveCompetitionInfo(compId));
        }

        public static async Task SaveClubsAsync(string[] clubsId)
        {
            Task[] tasks = new Task[clubsId.Length];
            int i = 0;
            foreach (var clubId in clubsId)
            {
                tasks[i] = SaveOneClubAsync(clubId);
                i++;
                await Task.Delay(50);
            }
            await Task.WhenAll(tasks);
        }

        public static async Task SaveOneClubAsync(string clubId)
        {
            await Task.Run(() => SaveOneClub(clubId));
        }

        public static async Task SavePlayersAsync(string[] playersId)
        {
            Task[] tasks = new Task[playersId.Length];
            int i = 0;
            foreach (var playerId in playersId)
            {
                tasks[i] = SaveOnePlayerAsync(playerId);
                i++;
                await Task.Delay(50);
            }
            await Task.WhenAll(tasks);
        }

        public static async Task SaveOnePlayerAsync(string playerId)
        {
            await Task.Run(() => SaveOnePlayer(playerId));
        }

        public static async Task SaveSeasonsClubsIdAsync(Season[] seasons)
        {
            Task[] tasks = new Task[seasons.Length];
            int i = 0;
            foreach (var season in seasons)
            {
                tasks[i] = SaveOneSeasonClubsIdAsync(season);
                i++;
                await Task.Delay(50);
            }
            await Task.WhenAll(tasks);
        }

        public static async Task SaveOneSeasonClubsIdAsync(Season season)
        {
            await Task.Run(() => SaveOneSeasonClubsId(season));
        }

        public static async Task SaveSeasonsPlayerStatisticsAsync(Season[] seasons)
        {
            using (AppContext db = new AppContext())
            {
                db.Database.ExecuteSqlRaw("TRUNCATE TABLE [PlayerStatistics]");
                Console.WriteLine("Удалены таблицы статистики игроков");
            }
            Thread.Sleep(3000);
            Task[] tasks = new Task[seasons.Length];
            int i = 0;
            foreach (var season in seasons)
            {
                tasks[i] = SaveOneSeasonPlayerStatisticsAsync(season);
                i++;
                await Task.Delay(150);
            }
            await Task.WhenAll(tasks);
        }

        public static async Task SaveOneSeasonPlayerStatisticsAsync(Season season)
        {
            await Task.Run(() => SaveOneSeasonPlayerStatistics(season));
        }

        public static async Task SaveSeasonsCompetitionTableAsync(Season[] seasons)
        {
            using (AppContext db = new AppContext())
            {
                db.Database.ExecuteSqlRaw("TRUNCATE TABLE [CompetitionTable]");
                Console.WriteLine("Удалены турнирные таблицы");
            }
            Thread.Sleep(3000);
            Task[] tasks = new Task[seasons.Length];
            int i = 0;
            foreach (var season in seasons)
            {
                tasks[i] = SaveOneSeasonCompetitionTableAsync(season);
                i++;
                await Task.Delay(150);
            }
            await Task.WhenAll(tasks);
        }

        public static async Task SaveOneSeasonCompetitionTableAsync(Season season)
        {
            await Task.Run(() => SaveOneSeasonCompetitionTable(season));
        }

        public static void SaveOneClub(string clubId)
        {
            using (AppContext db = new AppContext())
            {
                Console.WriteLine($"Клуб {clubId}");
                if (!db.Clubs.Any(x => x.Id == clubId))
                {
                    var clubInfo = GetClubInfoById(clubId);
                    db.Clubs.Add(clubInfo);
                    Console.WriteLine($"Клубов загружено: {clubId} ({db.Clubs.Count()})");
                }
                try
                {
                    db.SaveChanges();
                }
                catch(SqlException)
                {
                    Console.WriteLine($"Клуб {clubId} уже есть в БД");
                }
            }
        }

        public static void SaveOnePlayer(string playerId)
        {
            using (AppContext db = new AppContext())
            {
                Console.WriteLine($"Игрок {playerId}");
                if (!db.Players.Any(x => x.Id == playerId))
                {
                    var tuple = GetPlayerInfoById(playerId);
                    try
                    {
                        db.Players.Add(tuple.player);
                        FootballClubPlayer clubPlayer = new FootballClubPlayer { ClubId = tuple.clubId, PlayerId = playerId };
                        if (tuple.clubId == null || tuple.clubId == "")
                        {
                            Console.WriteLine($"У игрока {playerId} нет клуба.");
                        }
                        else if (!db.ClubsPlayers.Contains(clubPlayer))
                        {
                            db.ClubsPlayers.Add(clubPlayer);
                            Console.WriteLine($"Игрок загружен: {playerId} ({db.Players.Count()})");
                        }
                        db.SaveChanges();
                    }
                    catch (DbUpdateException)
                    {
                        Console.WriteLine($"Игрок {playerId} играет в клубе {tuple.clubId} которого нет в БД. Исправляем!");
                        if (tuple.clubId != null)
                        {
                            SaveOneClub(tuple.clubId);
                            Console.WriteLine($"Клуб {tuple.clubId} успешно добавлен");
                            SaveOnePlayer(playerId);
                        }
                    }

                }
            }
        }

        public static void SaveCompetitionInfo(string compId)
        {
            using (AppContext db = new AppContext())
            {
                var tuple = GetCompetitionInfo(compId);
                if (!db.Competitions.Any(x => x.Id == compId))
                {
                    db.Competitions.Add(tuple.comp);
                }
                db.SaveChanges();
                Console.WriteLine($"Добавлено лиг: {db.Competitions.Count()}");

                foreach (var seasonYear in tuple.seasonsYear)
                {
                    if (!db.Seasons.Any(x => x.CompetitionId == compId && x.Year == seasonYear))
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
                Console.WriteLine($"Добавлено сезонов: {db.Seasons.Count()}");
            }
        }

        public static void SaveOneSeasonPlayerStatistics(Season season)
        {
            using (AppContext db = new AppContext())
            {
                var c = new SqlCommand();
                c.CommandTimeout = 0;
                var playerStatistics = GetSeasonPlayerStatisticsById(season.CompetitionId, season.Id, season.Year);
                db.PlayerStatistics.AddRange(playerStatistics);
                db.SaveChanges();
                Console.WriteLine($"Добавлено строк статистики игроков: {db.PlayerStatistics.Count()}");
            }
        }

        public static void SaveOneSeasonCompetitionTable(Season season)
        {
            using (AppContext db = new AppContext())
            {
                var c = new SqlCommand();
                c.CommandTimeout = 0;
                var table = GetCompetitionTableById(season.CompetitionId, season.Id, season.Year);
                if (table != null)
                {
                    db.CompetitionTable.AddRange(table);
                }
                db.SaveChanges();
                Console.WriteLine($"Добавлено строк турнирных таблиц: {db.CompetitionTable.Count()}");
            }
        }

        public static void SaveOneSeasonClubsId(Season season)
        {
            using (AppContext db = new AppContext())
            {
                var clubsId = GetClubsIdInLeague(season.CompetitionId, season.Year);
                foreach (var clubId in clubsId)
                {
                    try
                    {
                        if (!db.ClubsSeasons.Any(x => x.ClubId == clubId && x.SeasonId == season.Id))
                        {
                            Console.WriteLine($"Клуб {clubId} привязываем к сезону {season.Id}");
                            var clubSeason = new FootballClubSeason() { ClubId = clubId, SeasonId = season.Id };
                            db.ClubsSeasons.Add(clubSeason);
                            db.SaveChanges();
                        }
                        else
                        {
                            Console.WriteLine($"Клуб {clubId} уже привязан к сезону {season.Id}");
                        }
                    }
                    catch (DbUpdateException)
                    {
                        if (!db.ClubsSeasons.Any(x => x.ClubId == clubId))
                        {
                            Console.WriteLine($"Клуба {clubId} нет в БД. Исправляем.");
                            var clubInfo = GetClubInfoById(clubId);
                            db.Clubs.Add(clubInfo);
                            try
                            {
                                db.SaveChanges();
                            }
                            catch (SqlException)
                            {
                                Console.WriteLine($"Клуб {clubId} уже есть в БД");
                            }
                            Console.WriteLine($"Связь между клубом {clubId} и сезоном {season.Id} была установлена.");
                        }
                    }
                }
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

        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            SaveDB(new List<string> { "12", "13", "14", "15", "16", "17", "18" });
            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine($"Время работы: { elapsedTime}");
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
        public List<PlayerStatistics> PlayerStatistics { get; set; }

        public List<CompetitionTable> Table { get; set; } = new List<CompetitionTable>();
        public List<FootballClubSeason> ClubsSeasons { get; set; } = new List<FootballClubSeason>();
    }

    class PlayerStatistics
    {
        public int Id { get; set; }

        public int SeasonId { get; set; }
        [ForeignKey("SeasonId")]
        public Season Season { get; set; }

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
        public List<FootballClubSeason> ClubsSeasons { get; set; } = new List<FootballClubSeason>();
    }

    class FootballClubSeason
    {
        public string ClubId { get; set; }
        [ForeignKey("ClubId")]
        public FootballClub Club { get; set; }
        public int SeasonId { get; set; }
        [ForeignKey("SeasonId")]
        public Season Season { get; set; }
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
        public List<PlayerStatistics> PlayerStatistics { get; set; }
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

        public DbSet<PlayerStatistics> PlayerStatistics { get; set; }

        public DbSet<CompetitionTable> CompetitionTable { get; set; }

        public DbSet<FootballClubSeason> ClubsSeasons { get; set; }

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

            modelBuilder.Entity<FootballClubSeason>()
                .HasKey(t => new { t.ClubId, t.SeasonId });
            modelBuilder.Entity<FootballClubSeason>()
                .HasOne(c => c.Club).WithMany(c => c.ClubsSeasons).HasForeignKey(c => c.ClubId);
            modelBuilder.Entity<FootballClubSeason>()
                .HasOne(s => s.Season).WithMany(s => s.ClubsSeasons).HasForeignKey(s => s.SeasonId);

        }
    }
}
