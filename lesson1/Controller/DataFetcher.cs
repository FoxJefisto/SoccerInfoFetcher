using HtmlAgilityPack;
using lesson1.Model;
using Microsoft.Data.SqlClient.DataClassification;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace lesson1.Controller
{
    class DataFetcher
    {
        private static DataFetcher instance;
        private RestClient restClient;
        public enum SearchScope
        {
            coaches,
            players,
            clubs,
            games,
            competitions,
            data
        }

        public static DataFetcher GetInstance()
        {
            if (instance == null)
            {
                instance = new DataFetcher();
            }
            return instance;
        }

        /// <summary>
        /// Получает и возвращает информацию о клубе. Осуществляет парсинг по html строке
        /// </summary>
        /// <param name="htmlCode"></param>
        /// <param name="clubId"></param>
        /// <returns></returns>
        public FootballClub GetClubInfo(string htmlCode, string clubId)
        {
            HtmlDocument doc = new HtmlDocument();
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
            string name, englishName, fullName, city, country, foundationDate, imgSource;
            name = englishName = fullName = city = country = foundationDate = imgSource = null;
            int? rating = null;
            if (doc.DocumentNode.SelectSingleNode("//div[@class='profile_foto width150']/img") is HtmlNode node)
            {
                imgSource = node.GetAttributeValue("src", null);
            }
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
                                        country = pair[1].Replace("&nbsp;", string.Empty);
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
                    ImgSource = imgSource,
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
        public FootballClub GetClubInfoById(string clubId)
        {
            string htmlCode = GetHTMLInfo(clubId, SearchScope.clubs);
            return GetClubInfo(htmlCode, clubId);
        }

        /// <summary>
        /// Возвращает id игроков, которые играют в клубе с данным clubId
        /// </summary>
        /// <param name="clubId"></param>
        /// <returns></returns>
        public List<string> GetPlayersIdInClub(string clubId)
        {
            string htmlCode = GetHTMLInfo(clubId, SearchScope.clubs, "&tab=squads");
            HtmlDocument doc = new HtmlDocument();
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
        public Player GetPlayerInfo(string htmlCode, string playerId)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlCode);
            var nodeName = doc.DocumentNode.SelectSingleNode(".//div[@class='profile_info new']/h1[@class='profile_info_title red']");
            Player player = null;
            string firstName, lastName, fullName, citizenship, placeOfBirth,
                position, workingLeg, imgSource;
            firstName = lastName = fullName = citizenship = placeOfBirth = position = workingLeg = imgSource = null;
            int? height, weight;
            height = weight = null;
            DateTime? dateOfBirth = null;
            if (doc.DocumentNode.SelectSingleNode("//div[@class='profile_foto width150']/img") is HtmlNode node)
            {
                imgSource = node.GetAttributeValue("src", null);
            }
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
                            case "Дата рождения":
                                dateOfBirth = DateTime.Parse(value.Split(' ')[0]);
                                break;
                            case "Гражданство":
                            case "Сборная":
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
                player = new Player()
                {
                    Id = playerId,
                    ImgSource = imgSource,
                    FirstName = firstName,
                    LastName = lastName,
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

        /// <summary>
        /// Возвращает экземпляр класса Player и Id его клуба
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public Player GetPlayerInfoById(string playerId)
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
        public string GetHTMLInfo(string id, SearchScope scope, string data1 = null, string data2 = null)
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

            if (address == null)
            {
                return "";
            }
            else
            {
                return restClient.GetStringAsync(address).Result;
            }

        }

        /// <summary>
        /// Получить клубы определенного сезона
        /// </summary>
        /// <param name="compId">id Соревнования</param>
        /// <param name="season">Год сезона</param>
        /// <returns></returns>
        public List<string> GetClubsIdInLeague(string compId, string season)
        {
            string htmlCode = GetHTMLInfo(compId, SearchScope.competitions, $"{season}/teams/");
            HtmlDocument doc = new HtmlDocument();
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
        public (Competition comp, List<string> seasonsYear) GetCompetitionInfo(string compId)
        {
            string htmlCode = GetHTMLInfo(compId, SearchScope.competitions);
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlCode);
            string compName = doc.DocumentNode.SelectSingleNode(".//h1[@class='profile_info_title red']").InnerText;
            var nodesInfo = doc.DocumentNode.SelectNodes(".//table[@class='profile_params']/tbody/tr");
            string imgSource = null;
            if (doc.DocumentNode.SelectSingleNode("//div[@class='profile_foto width64']/img") is HtmlNode node)
            {
                imgSource = node.GetAttributeValue("src", null);
            }
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
                ImgSource = imgSource,
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
        public (List<string> playersId, List<string> clubsId) GetAllPlayersAndClubsIdInSeason(string compId, string season = "")
        {
            string htmlCode = GetHTMLInfo(compId, SearchScope.competitions, $"{season}/players/");
            if (season == "")
                season = Regex.Match(htmlCode, @"selectbox-label"">([^<]+)").Groups[1].Value;
            string cp_ss = Regex.Match(htmlCode, @"cp_ss=([\d]+)").Groups[1].Value;
            htmlCode = GetHTMLInfo("", SearchScope.data, $"?c=competitions&a=tab_tablesorter_players&cp_ss={cp_ss}&cl=0&page=0&size=0&col[1]=1&col[4]=0");
            var json = JObject.Parse(htmlCode);
            var playersId = new List<string>();
            var clubsId = new List<string>();
            var doc = new HtmlDocument();
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

        public async Task<(List<string> playersId, List<string> clubsId)> GetAllPlayersAndClubsIdInSeasonAsync(string compId, string season = "")
        {
            return await Task.Run(() => GetAllPlayersAndClubsIdInSeason(compId, season));
        }

        /// <summary>
        /// Возвращает статистику игроков в заданном соревновании и заданном сезоне
        /// </summary>
        /// <param name="compId">id соревнования</param>
        /// <param name="seasonId">id сезона</param>
        /// <param name="season">год сезона</param>
        /// <returns></returns>
        public List<PlayerStatistics> GetSeasonPlayerStatisticsById(string compId, int seasonId, string season = "")
        {
            string htmlCode = GetHTMLInfo(compId, SearchScope.competitions, $"{season}/players/");
            if (season == "")
                season = Regex.Match(htmlCode, @"selectbox-label"">([^<]+)").Groups[1].Value;
            string cp_ss = Regex.Match(htmlCode, @"cp_ss=([\d]+)").Groups[1].Value;
            htmlCode = GetHTMLInfo("", SearchScope.data, $"?c=competitions&a=tab_tablesorter_players&cp_ss={cp_ss}&cl=0&page=0&size=0&col[1]=1&col[4]=0");
            var json = JObject.Parse(htmlCode);
            var playersStats = new List<PlayerStatistics>();
            var doc = new HtmlDocument();
            foreach (var row in json["rows"])
            {
                string playerId = null, clubId = null, labelPlayer = null;
                int? number = null;
                string htmlNameInfo = row[0].ToString();
                doc.LoadHtml(htmlNameInfo);
                var matchNumber = Regex.Match(htmlNameInfo, @"<br/>#(\d*)");
                if (matchNumber.Success)
                {
                    number = int.Parse(matchNumber.Groups[1].Value);
                }
                var hrefCell = doc.DocumentNode.SelectSingleNode("//a[@class='name']");
                if (hrefCell != null)
                {
                    string hrefStr = hrefCell.GetAttributeValue("href", "");
                    Match matchPlayerId = Regex.Match(hrefStr, @".*/(\d+)/");
                    if (matchPlayerId.Groups[1].Success)
                        playerId = matchPlayerId.Groups[1].Value;
                    labelPlayer = hrefCell.InnerText;
                }
                else
                {
                    hrefCell = doc.DocumentNode.SelectSingleNode("//span[@class='name']");
                    if (hrefCell != null)
                    {
                        labelPlayer = hrefCell.InnerText.Trim();
                    }
                }
                Match matchClubId = Regex.Match(htmlNameInfo, @"clubs/([\d]+)");
                if (matchClubId.Groups[1].Success)
                {
                    clubId = matchClubId.Groups[1].Value;
                    var stats = Array.ConvertAll(row.Skip(1).ToArray(), x => { if (x.ToString() != "&nbsp;") return (int)x; else return 0; });
                    var playerStats = new PlayerStatistics
                    {
                        PlayerId = playerId,
                        ClubId = clubId,
                        SeasonId = seasonId,
                        Label = labelPlayer,
                        Number = number,
                        Goals = stats[0],
                        Assists = stats[1],
                        Matches = stats[2],
                        Minutes = stats[3],
                        GoalPlusPass = stats[4],
                        PenGoals = stats[5],
                        DoubleGoals = stats[6],
                        HatTricks = stats[7],
                        OwnGoals = stats[8],
                        YellowCards = stats[9],
                        YellowRedCards = stats[10],
                        RedCards = stats[11],
                        FairPlayScore = stats[12]
                    };
                    playersStats.Add(playerStats);
                }

            }
            return playersStats;
        }

        public List<CompetitionTable> GetCompetitionTableById(string id, int seasonId, string season)
        {
            string htmlCode = GetHTMLInfo(id, SearchScope.competitions, season + "/");
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlCode);
            List<CompetitionTable> table = new List<CompetitionTable>();
            var nodesTable = doc.DocumentNode.SelectNodes(".//table[@class='stngs']");
            if (nodesTable == null)
                return table;
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

        public async Task<(List<string> playersId, List<string> clubsId)> GetAllPlayersAndClubsIdAsync(List<Season> seasons)
        {
            var tasks = new Task<(List<string> playersId, List<string> clubsId)>[seasons.Count];
            int i = 0;
            foreach (var season in seasons)
            {
                tasks[i] = GetAllPlayersAndClubsIdInSeasonAsync(season.CompetitionId, season.Year);
                tasks[i].ContinueWith(task => { Console.WriteLine($"Получено: {task.Result.clubsId.Count} клубов, {task.Result.playersId.Count} игроков"); });
                i++;
                await Task.Delay(50);
            }
            await Task.WhenAll(tasks);
            Console.WriteLine("Создание результирующего объекта");
            var clubsId = new List<string>();
            var playersId = new List<string>();
            foreach (var task in tasks)
            {
                clubsId.AddRange(task.Result.clubsId);
                playersId.AddRange(task.Result.playersId);
            }
            return (playersId, clubsId);
        }

        public List<string> GetMainCompsId(int page = 1)
        {
            var compsId = new List<string>();
            if (page == 2) return compsId;
            string htmlCode = GetHTMLInfo(null, SearchScope.data, "index.php?c=competitions&a=champs_list_data&tp=0&cn_id=0&st=0&ttl=&p=", page.ToString());
            HtmlDocument doc = new HtmlDocument();
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

        public async Task<(List<string> matchesId, Dictionary<string, int> matchIdToSeasonId)> GetAllUpcomingMatchesIdBySeasonsAsync(List<Season> currentSeasons)
        {
            var tasks = new Task<List<string>>[currentSeasons.Count];
            int i = 0;
            foreach (var season in currentSeasons)
            {
                tasks[i] = GetAllUpcomingMatchesIdBySeasonAsync(season);
                tasks[i].ContinueWith(task => { Console.WriteLine($"Получено предстоящих матчей: {task.Result.Count} матчей"); });
                i++;
                await Task.Delay(50);
            }
            await Task.WhenAll(tasks);
            var matchIdToSeasonId = new Dictionary<string, int>();
            for (i = 0; i < currentSeasons.Count; i++)
            {
                foreach (var matchId in tasks[i].Result)
                {
                    matchIdToSeasonId[matchId] = currentSeasons[i].Id;
                }
            }
            Console.WriteLine("Создание результирующего объекта");
            var matchesId = new List<string>();
            foreach (var task in tasks)
            {
                matchesId.AddRange(task.Result);
            }
            return (matchesId, matchIdToSeasonId);
        }

        public async Task<(List<string> matchesId, Dictionary<string, int> matchIdToSeasonId)> GetAllPastMatchesIdBySeasonsAsync(List<Season> seasons)
        {
            var tasks = new Task<List<string>>[seasons.Count];
            int i = 0;
            foreach (var season in seasons)
            {
                tasks[i] = GetAllPastMatchesIdBySeasonAsync(season);
                tasks[i].ContinueWith(task => { Console.WriteLine($"Получено прошедших матчей: {task.Result.Count} матчей"); });
                i++;
                await Task.Delay(50);
            }
            await Task.WhenAll(tasks);
            var matchIdToSeasonId = new Dictionary<string, int>();
            for(i = 0; i < seasons.Count; i++)
            {
                foreach(var matchId in tasks[i].Result)
                {
                    matchIdToSeasonId[matchId] = seasons[i].Id;
                }
            }
            Console.WriteLine("Создание результирующего объекта");
            var matchesId = new List<string>();
            foreach (var task in tasks)
            {
                matchesId.AddRange(task.Result);
            }
            return (matchesId, matchIdToSeasonId);
        }

        public async Task<List<string>> GetAllPastMatchesIdBySeasonAsync(Season season)
        {
            return await Task.Run(() => GetAllPastMatchesIdBySeason(season));
        }

        public async Task<List<string>> GetAllUpcomingMatchesIdBySeasonAsync(Season season)
        {
            return await Task.Run(() => GetAllUpcomingMatchesIdBySeason(season));
        }

        public List<string> GetAllUpcomingMatchesIdBySeason(Season season)
        {
            string htmlCode = GetHTMLInfo(season.CompetitionId, SearchScope.competitions, $"{season.Year}/shedule/");
            return GetAllMatchesIdByHtml(htmlCode);
        }

        public List<string> GetAllPastMatchesIdBySeason(Season season)
        {
            string htmlCode = GetHTMLInfo(season.CompetitionId, SearchScope.competitions, $"{season.Year}/results/");
            return GetAllMatchesIdByHtml(htmlCode);
        }

        public List<string> GetAllMatchesIdByHtml(string htmlCode)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlCode);
            var nodesLink = doc.DocumentNode.SelectNodes("//a[@class='game_link']//div[@class='icon']/span");
            var matches = new List<string>();
            if(nodesLink != null)
            {
                foreach (var nodeLink in nodesLink)
                {
                    var link = nodeLink.GetAttributeValue("onclick", "");
                    var matchMatchId = Regex.Match(link, @"showgame\((\d+)\)");
                    if (matchMatchId.Success)
                    {
                        matches.Add(matchMatchId.Groups[1].Value);
                    }
                }
            }
            return matches;
        }

        public FootballMatch GetMatchByHtml(string htmlCode, string matchId)
        {
            var match = new FootballMatch();
            match.Id = matchId;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlCode);
            var home = new MatchStatistics();
            var away = new MatchStatistics();
            var nodesGoals = doc.DocumentNode.SelectNodes("//div[@class='live_game_goal']");
            if (nodesGoals.Count == 2)
            {
                var homeGoals = nodesGoals[0].InnerText.Trim();
                var awayGoals = nodesGoals[1].InnerText.Trim();
                home.Goals = homeGoals == "-" ? null : Convert.ToInt32(homeGoals);
                away.Goals = awayGoals == "-" ? null : Convert.ToInt32(awayGoals);
            }
            var nodeGameStatus = doc.DocumentNode.SelectSingleNode("//div[@class='live_game_status']");
            if(nodeGameStatus is not null)
            {
                match.Status = nodeGameStatus.InnerText.Trim();
            }
            else
            {
                match.Status = "Ожидается";
            }
            var nodeHome = doc.DocumentNode.SelectSingleNode("//div[@class='live_game_ht']/a");
            var nodeAway = doc.DocumentNode.SelectSingleNode("//div[@class='live_game_at']/a");
            if (nodeHome is not null && nodeAway is not null)
            {
                var href = nodeHome.GetAttributeValue("href", "");
                var matchClubId = Regex.Match(href, @"/clubs/(\d+)/");
                if (matchClubId.Success)
                {
                    home.ClubId = matchClubId.Groups[1].Value;
                    home.HomeAway = HomeAway.Home;
                }
                href = nodeAway.GetAttributeValue("href", "");
                matchClubId = Regex.Match(href, @"/clubs/(\d+)/");
                if (matchClubId.Success)
                {
                    away.ClubId = matchClubId.Groups[1].Value;
                    away.HomeAway = HomeAway.Away;
                }
            }
            var nodeLabel = doc.DocumentNode.SelectSingleNode("//div[@class='block_header bkcenter']");
            if (nodeLabel is not null)
            {
                match.Label = nodeLabel.InnerText.Trim();
                var str = match.Label.Split(", ");
                match.Stage = str[1];
                try
                {
                    match.Date = DateTime.ParseExact(str[str.Length - 1], "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
                }
                catch
                {
                    try
                    {
                        match.Date = DateTime.Parse(str[str.Length - 1]);
                    }
                    catch
                    {
                        var matchDate = Regex.Match(htmlCode, @"startDate"":""([^T]+)T([^\+]+)");
                        if (matchDate.Success)
                        {
                            var date = $"{matchDate.Groups[1].Value} {matchDate.Groups[2].Value}";
                            match.Date = DateTime.ParseExact(date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            match.Date = null;
                        }
                    }
                }
            }
            var nodesStatsTitle = doc.DocumentNode.SelectNodes("//div[@class='stats_title']");
            var nodesStats = doc.DocumentNode.SelectNodes("//div[@class='stats_inf']");
            if (nodesStats is not null)
            {
                var nodesStatsHome = nodesStats.Where((x, i) => i % 2 == 0).ToArray();
                var nodesStatsAway = nodesStats.Where((x, i) => i % 2 == 1).ToArray();
                for (int i = 0; i < nodesStatsTitle.Count; i++)
                {
                    switch (nodesStatsTitle[i].InnerText)
                    {
                        case "xG":
                            home.Xg = Convert.ToDouble(nodesStatsHome[i].InnerText, CultureInfo.InvariantCulture);
                            away.Xg = Convert.ToDouble(nodesStatsAway[i].InnerText, CultureInfo.InvariantCulture);
                            break;
                        case "Удары":
                            home.Shots = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.Shots = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Удары в створ":
                            home.ShotsOnTarget = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.ShotsOnTarget = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Блок-но ударов":
                            home.ShotsBlocked = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.ShotsBlocked = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Сейвы":
                            home.Saves = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.Saves = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Владение %":
                            home.BallPossession = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.BallPossession = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Угловые":
                            home.Corners = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.Corners = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Нарушения":
                            home.Fouls = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.Fouls = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Офсайды":
                            home.Offsides = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.Offsides = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Желтые карточки":
                            home.YCards = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.YCards = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Красные карточки":
                            home.RCards = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.RCards = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Атаки":
                            home.Attacks = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.Attacks = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Опасные атаки":
                            home.AttacksDangerous = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.AttacksDangerous = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Передачи":
                            home.Passes = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.Passes = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Точность передач %":
                            home.AccPasses = Convert.ToDouble(nodesStatsHome[i].InnerText, CultureInfo.InvariantCulture);
                            away.AccPasses = Convert.ToDouble(nodesStatsAway[i].InnerText, CultureInfo.InvariantCulture);
                            break;
                        case "Штрафные удары":
                            home.FreeKicks = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.FreeKicks = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Вбрасывания":
                            home.Prowing = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.Prowing = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Навесы":
                            home.Crosses = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.Crosses = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                        case "Отборы":
                            home.Tackles = Convert.ToInt32(nodesStatsHome[i].InnerText);
                            away.Tackles = Convert.ToInt32(nodesStatsAway[i].InnerText);
                            break;
                    }
                }
            }
            match.Statistics = new List<MatchStatistics>() { home, away };
            return match;
        }

        public List<MatchEvent>[] GetMatchEventsByHtml(string htmlCode,List<MatchSquadPlayers>[] squad)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlCode);
            var matchHomeEvents = new List<MatchEvent>();
            var nodesHomeEvents = doc.DocumentNode.SelectNodes("//div[@class='event_ht' and div]");
            if(nodesHomeEvents is not null)
            {
                foreach (var nodeHomeEvent in nodesHomeEvents)
                {
                    var matchEvent = new MatchEvent();
                    var nodeMinute = nodeHomeEvent.SelectSingleNode("../div[@class='event_min']");
                    if (nodeMinute is not null)
                    {
                        matchEvent.Minute = nodeMinute.InnerText.Trim();
                    }
                    var nodeEventType = nodeHomeEvent.SelectSingleNode("./div[contains(@class,'event_ht_icon')]");
                    if (nodeEventType is not null)
                    {
                        matchEvent.Type = nodeEventType.GetClasses().Skip(1).First().Replace("live_", "");
                    }
                    var nodeAssist = nodeHomeEvent.SelectSingleNode("./span[@class='gray assist']");
                    if (nodeAssist is not null)
                    {
                        var matchAssist = new MatchEvent();
                        matchAssist.Minute = matchEvent.Minute;
                        matchAssist.Type = "assist";
                        matchAssist.Label = nodeAssist.InnerText.Trim();
                        var matchName = Regex.Match(matchAssist.Label, @"([^\.]*\. )*(.*)");
                        if (matchName.Groups[2].Success)
                        {
                            var name = matchName.Groups[2].Value;
                            if(squad[0].FirstOrDefault(x => x.Label.Contains(name)) is MatchSquadPlayers msp)
                            {
                                matchAssist.PlayerId = msp.PlayerId;
                            }
                            
                        }
                        matchHomeEvents.Add(matchAssist);
                        nodeAssist.Remove();
                    }
                    var nodeAuthor = nodeHomeEvent.SelectSingleNode(".//a[@href]");
                    if (nodeAuthor is not null)
                    {
                        var href = nodeAuthor.GetAttributeValue("href", "");
                        var matchPlayerId = Regex.Match(href, @"/players/(\d+)/");
                        if (matchPlayerId.Success)
                        {
                            matchEvent.PlayerId = matchPlayerId.Groups[1].Value;
                        }
                    }
                    matchEvent.Label = nodeHomeEvent.InnerText.Trim();
                    matchHomeEvents.Add(matchEvent);
                }
            }
           
            var matchAwayEvents = new List<MatchEvent>();
            var nodesAwayEvents = doc.DocumentNode.SelectNodes("//div[@class='event_at' and div]");
            if(nodesAwayEvents is not null)
            {
                foreach (var nodeAwayEvent in nodesAwayEvents)
                {
                    var matchEvent = new MatchEvent();
                    var nodeMinute = nodeAwayEvent.SelectSingleNode("../div[@class='event_min']");
                    if (nodeMinute is not null)
                    {
                        matchEvent.Minute = nodeMinute.InnerText.Trim();
                    }
                    var nodeEventType = nodeAwayEvent.SelectSingleNode("./div[contains(@class,'event_at_icon')]");
                    if (nodeEventType is not null)
                    {
                        matchEvent.Type = nodeEventType.GetClasses().Skip(1).First().Replace("live_", "");
                    }
                    var nodeAssist = nodeAwayEvent.SelectSingleNode("./span[@class='gray assist']");
                    if (nodeAssist is not null)
                    {
                        var matchAssist = new MatchEvent();
                        matchAssist.Minute = matchEvent.Minute;
                        matchAssist.Type = "assist";
                        matchAssist.Label = nodeAssist.InnerText.Trim();
                        var matchName = Regex.Match(matchAssist.Label, @"([^\.]*\. )*(.*)");
                        if (matchName.Groups[2].Success)
                        {
                            var name = matchName.Groups[2].Value;
                            if (squad[1].FirstOrDefault(x => x.Label.Contains(name)) is MatchSquadPlayers msp)
                            {
                                matchAssist.PlayerId = msp.PlayerId;
                            }
                        }
                        matchAwayEvents.Add(matchAssist);
                        nodeAssist.Remove();
                    }
                    var nodeAuthor = nodeAwayEvent.SelectSingleNode(".//a[@href]");
                    if (nodeAuthor is not null)
                    {
                        var href = nodeAuthor.GetAttributeValue("href", "");
                        var matchPlayerId = Regex.Match(href, @"/players/(\d+)/");
                        if (matchPlayerId.Success)
                        {
                            matchEvent.PlayerId = matchPlayerId.Groups[1].Value;
                        }
                    }
                    matchEvent.Label = nodeAwayEvent.InnerText.Trim();
                    matchAwayEvents.Add(matchEvent);
                }
            }
            return new List<MatchEvent>[] { matchHomeEvents, matchAwayEvents };
        }

        public List<MatchSquadPlayers>[] GetMatchSquadByHtml(string htmlCode)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlCode);
            var nodesLineUpSquads = doc.DocumentNode.SelectNodes("//div[@id='tm-lineup']/div[@class='сomposit_block' and position()<=2]");
            var nodeType = doc.DocumentNode.SelectSingleNode(".//div[@id='tablp-lineup']");
            var squads = new List<MatchSquadPlayers>[]
            {
                new List<MatchSquadPlayers>(),
                new List<MatchSquadPlayers>()
            };
            if(nodesLineUpSquads is not null)
            {
                for (int i = 0; i < nodesLineUpSquads.Count; i++)
                {
                    var nodesPlayerRows = nodesLineUpSquads[i].SelectNodes(".//tr");
                    foreach(var nodePlayerRow in nodesPlayerRows)
                    {
                        var player = new MatchSquadPlayers();
                        var nodeNumber = nodePlayerRow.SelectSingleNode(".//span[@class='сomposit_num']");
                        if(nodeNumber is not null)
                        {
                            try
                            {
                                player.Number = Convert.ToInt32(nodeNumber.InnerText.Trim());
                            }
                            catch { }
                        }
                        var nodePlayer = nodePlayerRow.SelectSingleNode(".//a[@href]");
                        if(nodePlayer is not null)
                        {
                            var href = nodePlayer.GetAttributeValue("href", "");
                            var matchPlayerId = Regex.Match(href, @"/players/(\d+)/");
                            if (matchPlayerId.Success)
                            {
                                player.PlayerId = matchPlayerId.Groups[1].Value;
                            }
                        }
                        player.Label = nodePlayerRow.SelectSingleNode(".//span[@class='сomposit_player']").InnerText.Trim();

                        if(nodeType is not null)
                        {
                            var type = nodeType.InnerText.Trim();
                            if (type == "Стартовые составы")
                            {
                                player.Type = SquadType.Lineup;
                            }
                            else if (type == "Вероятные составы")
                            {
                                player.Type = SquadType.Probably;
                            }
                            else
                                player.Type = SquadType.Unknown;
                        }

                        var nodeIsCaptain = nodePlayerRow.SelectSingleNode(".//span[contains(@class,'has-tip')]");
                        if(nodeIsCaptain is not null)
                        {
                            player.IsCaptain = true;
                        }
                        squads[i].Add(player);
                    }
                }   
            }
            var nodesSubsSquads = doc.DocumentNode.SelectNodes("//div[@id='tm-subst']/div[@class='сomposit_block']");
            if (nodesSubsSquads is not null)
            {
                for (int i = 0; i < nodesSubsSquads.Count; i++)
                {
                    var nodesPlayerRows = nodesSubsSquads[i].SelectNodes(".//tr");
                    if(nodesPlayerRows is not null)
                    {
                        foreach (var nodePlayerRow in nodesPlayerRows)
                        {
                            var player = new MatchSquadPlayers();
                            var nodeNumber = nodePlayerRow.SelectSingleNode(".//span[@class='сomposit_num']");
                            if (nodeNumber is not null)
                            {
                                try
                                {
                                    player.Number = Convert.ToInt32(nodeNumber.InnerText.Trim());
                                }
                                catch (Exception) { }
                            }
                            var nodePlayer = nodePlayerRow.SelectSingleNode(".//a[@href]");
                            if (nodePlayer is not null)
                            {
                                var href = nodePlayer.GetAttributeValue("href", "");
                                var matchPlayerId = Regex.Match(href, @"/players/(\d+)/");
                                if (matchPlayerId.Success)
                                {
                                    player.PlayerId = matchPlayerId.Groups[1].Value;
                                }
                            }
                            player.Label = nodePlayerRow.SelectSingleNode(".//span[@class='сomposit_player']").InnerText.Trim();
                            player.Type = SquadType.Substitute;
                            squads[i].Add(player);
                        }
                    }
                }
            }
            return squads;
        }

        private DataFetcher()
        {
            restClient = new RestClient();
        }
    }
}
