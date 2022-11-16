using HtmlAgilityPack;
using lesson1.Model;
using Microsoft.Data.SqlClient.DataClassification;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
                        AutoGoals = stats[8],
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

        private DataFetcher()
        {
            restClient = new RestClient();
        }
    }
}
