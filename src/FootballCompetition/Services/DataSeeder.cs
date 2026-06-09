using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FootballCompetition.Data;
using FootballCompetition.Models;

namespace FootballCompetition.Services;

/// <summary>
/// Seeds a fresh JSON store with a small but believable competition: four
/// teams, eight players each, four stadiums, and a six-match round so the
/// reports view has something to chew on the first time the app is launched.
/// </summary>
public static class DataSeeder
{
    public static void SeedIfEmpty(
        string dataDirectory,
        FileRepository<Team> teamRepo,
        FileRepository<Player> playerRepo,
        FileRepository<Stadium> stadiumRepo,
        FileRepository<Match> matchRepo)
    {
        // We only seed when ALL stores are empty; that way the user can wipe
        // a single file to get a partial reset without us clobbering the
        // others, and re-running the app on existing data never drops it.
        if (teamRepo.GetAll().Any() ||
            playerRepo.GetAll().Any() ||
            stadiumRepo.GetAll().Any() ||
            matchRepo.GetAll().Any())
        {
            return;
        }

        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }

        var stadiums = BuildStadiums();
        foreach (var s in stadiums) stadiumRepo.Add(s);

        var teams = BuildTeams(stadiums);
        foreach (var t in teams)
        {
            foreach (var p in t.Players) playerRepo.Add(p);
            teamRepo.Add(t);
        }

        var matches = BuildMatches(teams, stadiums);
        foreach (var m in matches) matchRepo.Add(m);
    }

    private static List<Stadium> BuildStadiums() => new()
    {
        new Stadium { Name = "Главная арена СК Шериф", City = "Тирасполь", Capacity = 12746, TicketPrice = 80m },
        new Stadium { Name = "Стадион Зимбру", City = "Кишинёв", Capacity = 10400, TicketPrice = 60m },
        new Stadium { Name = "Городской стадион Оргеева", City = "Оргеев", Capacity = 3000, TicketPrice = 40m },
        new Stadium { Name = "Городской стадион Хынчешт", City = "Хынчешты", Capacity = 1500, TicketPrice = 30m },
    };

    private static List<Team> BuildTeams(List<Stadium> stadiums)
    {
        Team Make(string name, string city, string coach, int rating, IEnumerable<Stadium> homes,
                  params (string fullName, int age, string role, int number)[] roster)
        {
            var t = new Team
            {
                Name = name,
                City = city,
                Coach = coach,
                LastSeasonRating = rating,
                Stadiums = homes.ToList(),
            };
            foreach (var (fn, age, role, num) in roster)
            {
                t.Players.Add(new Player { FullName = fn, Age = age, Role = role, Number = num });
            }
            return t;
        }

        return new()
        {
            Make("Шериф", "Тирасполь", "Роберто Бордин", 95,
                new[] { stadiums[0] },
                ("Дмитрий Челядник", 31, "Вратарь", 1),
                ("Габи Кики",        28, "Защитник", 3),
                ("Степан Раделич",   26, "Защитник", 15),
                ("Амин Талал",       25, "Полузащитник", 8),
                ("Жуан Пауло",       27, "Полузащитник", 10),
                ("Седрик Бадоло",    24, "Полузащитник", 20),
                ("Луваннор Энрике",  33, "Нападающий", 90),
                ("Рашид Аканби",     24, "Нападающий", 11)),

            Make("Зимбру", "Кишинёв", "Лилиан Попеску", 85,
                new[] { stadiums[1] },
                ("Игорь Мостовей",    24, "Вратарь", 12),
                ("Штефан Бургиу",     31, "Защитник", 3),
                ("Денис Дедечко",     35, "Полузащитник", 4),
                ("Александр Дедов",   34, "Полузащитник", 7),
                ("Никита Коваль",     21, "Полузащитник", 8),
                ("Владислав Сандуляк", 20, "Нападающий", 9),
                ("Эммануэль Аларибе", 22, "Нападающий", 11),
                ("Максим Фосэ",       25, "Защитник", 18)),

            Make("Милсами", "Оргеев", "Сергей Клещенко", 75,
                new[] { stadiums[2] },
                ("Эмил Тымбур",       25, "Вратарь", 1),
                ("Вадим Болохан",     36, "Защитник", 4),
                ("Игорь Ламбарский",  30, "Полузащитник", 8),
                ("Раду Гынсарь",      31, "Полузащитник", 10),
                ("Артём Пунтус",      28, "Нападающий", 9),
                ("Сергей Истрати",    32, "Нападающий", 11),
                ("Александр Антонюк", 33, "Нападающий", 17),
                ("Ярослав Терехов",   23, "Защитник", 23)),

            Make("Петрокуб", "Хынчешты", "Иван Табанов", 80,
                new[] { stadiums[3] },
                ("Кристиан Аврам",    28, "Вратарь", 1),
                ("Ион Жардан",        33, "Защитник", 90),
                ("Максим Потырнике",  33, "Защитник", 4),
                ("Виктор Богачук",    23, "Полузащитник", 8),
                ("Ясер Цуркан",       25, "Полузащитник", 80),
                ("Владимир Амброс",   29, "Нападающий", 9),
                ("Мариус Иосипой",    23, "Полузащитник", 11),
                ("Виктор Мудрак",     29, "Защитник", 21)),
        };
    }

    private static List<Match> BuildMatches(List<Team> teams, List<Stadium> stadiums)
    {
        // Round-robin-ish, partially played so the reports view has data.
        var sheriff = teams[0];
        var zimbru = teams[1];
        var milsami = teams[2];
        var petrocub = teams[3];

        var today = DateTime.Today;

        return new()
        {
            new Match
            {
                Team1Key = sheriff.Key, Team2Key = zimbru.Key,
                StadiumKey = stadiums[0].Key,
                Date = today.AddDays(-21), Score = "2:1",
            },
            new Match
            {
                Team1Key = milsami.Key, Team2Key = petrocub.Key,
                StadiumKey = stadiums[2].Key,
                Date = today.AddDays(-20), Score = "3:0",
            },
            new Match
            {
                Team1Key = sheriff.Key, Team2Key = milsami.Key,
                StadiumKey = stadiums[0].Key,
                Date = today.AddDays(-14), Score = "1:1",
            },
            new Match
            {
                Team1Key = zimbru.Key, Team2Key = petrocub.Key,
                StadiumKey = stadiums[1].Key,
                Date = today.AddDays(-13), Score = "2:0",
            },
            new Match
            {
                Team1Key = petrocub.Key, Team2Key = sheriff.Key,
                StadiumKey = stadiums[3].Key,
                Date = today.AddDays(-7), Score = "0:2",
            },
            new Match
            {
                Team1Key = zimbru.Key, Team2Key = milsami.Key,
                StadiumKey = stadiums[1].Key,
                Date = today.AddDays(7), Score = string.Empty,
            },
        };
    }
}
