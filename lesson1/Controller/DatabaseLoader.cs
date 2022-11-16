using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace lesson1.Controller
{
    class DatabaseLoader
    {
        private static DatabaseLoader instance;
        public DataFetcher dataFetcher;
        public int clubsExpectedLength;
        public int playersExpectedLength;

        public static DatabaseLoader GetInstance()
        {
            if (instance == null)
            {
                instance = new DatabaseLoader();
            }
            return instance;
        }

        public void LoadStatistics(List<string> compsId)
        {
            Season[] seasons;
            using (AppContext db = new AppContext())
            {
                seasons = db.Seasons.ToArray();
            }
            LoadSeasonsPlayerStatisticsAsync(seasons).GetAwaiter().GetResult();
            LoadSeasonsCompetitionTableAsync(seasons).GetAwaiter().GetResult();
        }

        public string[] FilterPlayersId(List<string> playersId)
        {
            using (AppContext db = new AppContext())
            {
                var result = playersId.Distinct().Except(db.Players.Select(x => x.Id)).ToArray();
                playersExpectedLength = result.Length + db.Players.Count();
                return result;
            }
        }

        public string[] FilterClubsId(List<string> clubsId)
        {
            using (AppContext db = new AppContext())
            {
                var result = clubsId.Distinct().Except(db.Clubs.Select(x => x.Id)).ToArray();
                clubsExpectedLength = result.Length + db.Clubs.Count();
                return result;
            }
        }

        public void LoadPlayersAndClubsInfo(List<string> compsId)
        {
            Console.WriteLine("Загрузка турниров...");
            LoadCompetitionsInfoAsync(compsId).GetAwaiter().GetResult();
            Season[] seasons;
            using (AppContext db = new AppContext())
            {
                seasons = db.Seasons.ToArray();
            }
            Console.WriteLine($"Сбор всех игроков и клубов с {seasons.Length} сезонов...");
            var tuple = dataFetcher.GetAllPlayersAndClubsIdAsync(seasons).GetAwaiter().GetResult();

            Console.WriteLine("Фильтрация клубов...");
            var unloadedClubsId = FilterClubsId(tuple.clubsId);
            Console.WriteLine("Фильтрация игроков...");
            var unloadedPlayersId = FilterPlayersId(tuple.playersId);
            Console.WriteLine($"Количество незагруженных клубов: {unloadedClubsId.Length}");
            Console.WriteLine($"Количество незагруженных игроков: {unloadedPlayersId.Length}");

            Console.WriteLine($"Сохранение {unloadedClubsId.Length} клубов...");
            LoadClubsAsync(unloadedClubsId.Where(x => x != null).ToArray()).GetAwaiter().GetResult();
            Console.WriteLine($"Сохранение {unloadedPlayersId.Length} игроков...");
            LoadPlayersAsync(unloadedPlayersId.Where(x => x != null).ToArray()).GetAwaiter().GetResult();
            Console.WriteLine("Привязка клубов к сезонам...");
            LoadSeasonsClubsIdAsync(seasons).GetAwaiter().GetResult();
        }

        public async Task LoadCompetitionsInfoAsync(List<string> compsId)
        {
            Task[] tasks = new Task[compsId.Count];
            int i = 0;
            foreach (var compId in compsId)
            {
                tasks[i] = LoadOneCompetitionInfoAsync(compId);
                i++;
            }
            await Task.WhenAll(tasks);
        }

        public async Task LoadOneCompetitionInfoAsync(string compId)
        {
            await Task.Run(() => LoadOneCompetitionInfo(compId));
        }

        public async Task LoadClubsAsync(string[] clubsId)
        {
            Task[] tasks = new Task[clubsId.Length];
            int i = 0;
            foreach (var clubId in clubsId)
            {
                tasks[i] = LoadOneClubAsync(clubId);
                i++;
                await Task.Delay(50);
            }
            await Task.WhenAll(tasks);
        }

        public async Task LoadOneClubAsync(string clubId)
        {
            await Task.Run(() => LoadOneClub(clubId));
        }

        public async Task LoadPlayersAsync(string[] playersId)
        {
            Task[] tasks = new Task[playersId.Length];
            int i = 0;
            foreach (var playerId in playersId)
            {
                tasks[i] = LoadOnePlayerAsync(playerId);
                i++;
                await Task.Delay(50);
            }
            await Task.WhenAll(tasks);
        }

        public async Task LoadOnePlayerAsync(string playerId)
        {
            await Task.Run(() => LoadOnePlayer(playerId));
        }

        public async Task LoadSeasonsClubsIdAsync(Season[] seasons)
        {
            Task[] tasks = new Task[seasons.Length];
            int i = 0;
            foreach (var season in seasons)
            {
                tasks[i] = LoadOneSeasonClubsIdAsync(season);
                i++;
                await Task.Delay(50);
            }
            await Task.WhenAll(tasks);
        }

        public async Task LoadOneSeasonClubsIdAsync(Season season)
        {
            await Task.Run(() => LoadOneSeasonClubsId(season));
        }

        public async Task LoadSeasonsPlayerStatisticsAsync(Season[] seasons)
        {
            Task[] tasks = new Task[seasons.Length];
            int i = 0;
            foreach (var season in seasons)
            {
                tasks[i] = LoadOneSeasonPlayerStatisticsAsync(season);
                i++;
                await Task.Delay(150);
            }
            await Task.WhenAll(tasks);
        }

        public async Task LoadOneSeasonPlayerStatisticsAsync(Season season)
        {
            await Task.Run(() => LoadOneSeasonPlayerStatistics(season));
        }

        public async Task LoadSeasonsCompetitionTableAsync(Season[] seasons)
        {
            Task[] tasks = new Task[seasons.Length];
            int i = 0;
            foreach (var season in seasons)
            {
                tasks[i] = LoadOneSeasonCompetitionTableAsync(season);
                i++;
                await Task.Delay(150);
            }
            await Task.WhenAll(tasks);
        }

        public async Task LoadOneSeasonCompetitionTableAsync(Season season)
        {
            await Task.Run(() => LoadOneSeasonCompetitionTable(season));
        }

        public void LoadOneClub(string clubId)
        {
            using (AppContext db = new AppContext())
            {
                if (!db.Clubs.Any(x => x.Id == clubId))
                {
                    var clubInfo = dataFetcher.GetClubInfoById(clubId);
                    db.Clubs.Add(clubInfo);
                    try
                    {
                        db.SaveChanges();
                        Console.WriteLine($"Клуб {clubInfo.Name} успешно загружен. Всего: {db.Clubs.Count()}/{clubsExpectedLength}");
                    }
                    catch (SqlException)
                    {
                        Console.WriteLine($"Клуб {clubId} уже есть в БД");
                    }
                }
            }
        }

        public void LoadOnePlayer(string playerId)
        {
            using (AppContext db = new AppContext())
            {
                if (!db.Players.Any(x => x.Id == playerId))
                {
                    var player = dataFetcher.GetPlayerInfoById(playerId);
                    db.Players.Add(player);
                    db.SaveChanges();
                    Console.WriteLine($"Игрок {player.OriginalName} успешно загружен. Всего: {db.Players.Count()}/{playersExpectedLength}");
                }
            }
        }

        public void LoadOneCompetitionInfo(string compId)
        {
            using (AppContext db = new AppContext())
            {
                var tuple = dataFetcher.GetCompetitionInfo(compId);
                if (!db.Competitions.Any(x => x.Id == compId))
                {
                    db.Competitions.Add(tuple.comp);
                    db.SaveChanges();
                    Console.WriteLine($"Турнир {tuple.comp.Name} {tuple.comp.Country} успешно загружен. Всего: {db.Competitions.Count()}");
                }

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
                        db.SaveChanges();
                        Console.WriteLine($"Cезон {season.Year} в турнир {tuple.comp.Name} {tuple.comp.Country} успешно загружен. Всего: {db.Seasons.Count()}");
                    }
                }
            }
        }

        public void LoadOneSeasonPlayerStatistics(Season season)
        {
            using (AppContext db = new AppContext())
            {
                db.PlayerStatistics.RemoveRange(db.PlayerStatistics.Where(x => x.SeasonId == season.Id));
                Console.WriteLine($"Старая таблица статистики игроков сезона {season.Id} {season.Year} успешно удалена. Всего: {db.PlayerStatistics.Count()}");
                var playerStatistics = dataFetcher.GetSeasonPlayerStatisticsById(season.CompetitionId, season.Id, season.Year);
                db.PlayerStatistics.AddRange(playerStatistics);
                db.SaveChanges();
                Console.WriteLine($"Таблица статистики игроков сезона {season.Id} {season.Year} успешно загружена. Всего: {db.PlayerStatistics.Count()}");
            }
        }

        public void LoadOneSeasonCompetitionTable(Season season)
        {
            using (AppContext db = new AppContext())
            {
                var table = dataFetcher.GetCompetitionTableById(season.CompetitionId, season.Id, season.Year);
                foreach (var row in table)
                {
                    var result = db.CompetitionTable.SingleOrDefault(x => x.ClubId == row.ClubId && x.SeasonId == row.SeasonId && x.GroupName == row.GroupName);
                    if (result is null)
                    {
                        if (!db.Clubs.Any(x => x.Id == row.ClubId || x.Id == null))
                        {
                            Console.WriteLine($"Клуба {row.ClubId} нет в БД. Исправляем...");
                            var clubInfo = dataFetcher.GetClubInfoById(row.ClubId);
                            db.Clubs.Add(clubInfo);
                            Console.WriteLine($"Клуб {row.ClubId} успешно загружен. Всего: {db.Clubs.Count()}");
                        }
                        db.CompetitionTable.Add(row);
                        db.SaveChanges();
                        Console.WriteLine($"Строка турнирной таблицы клуба {row.ClubId} успешно загружена. Всего: {db.CompetitionTable.Count()}");
                    }
                    else
                    {
                        row.Id = result.Id;
                        db.Entry(result).CurrentValues.SetValues(row);
                        db.SaveChanges();
                        Console.WriteLine($"Строка турнирной таблицы клуба {row.ClubId} успешно обновлена.");
                    }
                }
            }
        }

        public void LoadOneSeasonClubsId(Season season)
        {
            using (AppContext db = new AppContext())
            {
                var clubsId = dataFetcher.GetClubsIdInLeague(season.CompetitionId, season.Year);
                foreach (var clubId in clubsId)
                {
                    if (!db.ClubsSeasons.Any(x => x.ClubId == clubId && x.SeasonId == season.Id))
                    {
                        if (!db.Clubs.Any(x => x.Id == clubId))
                        {
                            Console.WriteLine($"Клуба {clubId} нет в БД. Исправляем...");
                            var clubInfo = dataFetcher.GetClubInfoById(clubId);
                            db.Clubs.Add(clubInfo);
                            Console.WriteLine($"Клуб {clubId} успешно загружен. Всего: {db.Clubs.Count()}");
                        }
                        var clubSeason = new FootballClubSeason() { ClubId = clubId, SeasonId = season.Id };
                        db.ClubsSeasons.Add(clubSeason);
                        db.SaveChanges();
                        Console.WriteLine($"Клуб {clubId} успешно привязан к сезону {season.Id}");
                    }
                }
            }
        }

        private DatabaseLoader()
        {
            dataFetcher = DataFetcher.GetInstance();
        }
    }
}
