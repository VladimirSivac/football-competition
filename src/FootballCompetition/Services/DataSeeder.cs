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
        new Stadium { Name = "Arena Național", City = "Bucharest", Capacity = 55000, TicketPrice = 60m },
        new Stadium { Name = "Steaua Stadium", City = "Bucharest", Capacity = 31000, TicketPrice = 45m },
        new Stadium { Name = "Cluj Arena",    City = "Cluj-Napoca", Capacity = 30200, TicketPrice = 40m },
        new Stadium { Name = "Ilie Oană",     City = "Ploiești",    Capacity = 15000, TicketPrice = 25m },
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
            Make("FCSB", "Bucharest", "Elias Charalambous", 95,
                new[] { stadiums[0], stadiums[1] },
                ("Andrei Vlad",     27, "Goalkeeper", 1),
                ("Vlad Chiricheș",  34, "Defender",   3),
                ("Risto Radunović", 32, "Defender",   13),
                ("Mihai Lixandru",  21, "Midfielder", 6),
                ("Darius Olaru",    26, "Midfielder", 10),
                ("Florin Tănase",   29, "Midfielder", 21),
                ("Daniel Bîrligea", 24, "Forward",    9),
                ("David Miculescu", 23, "Forward",    11)),

            Make("CFR Cluj", "Cluj-Napoca", "Dan Petrescu", 88,
                new[] { stadiums[2] },
                ("Otto Hindrich",     22, "Goalkeeper", 1),
                ("Andrei Burcă",      31, "Defender",   2),
                ("Karlo Muhar",       27, "Midfielder", 8),
                ("Alex Chipciu",      35, "Midfielder", 16),
                ("Louis Munteanu",    22, "Forward",    99),
                ("Dan Nistor",        36, "Midfielder", 10),
                ("Jonathan Cisse",    28, "Defender",   23),
                ("Ovidiu Bic",        29, "Midfielder", 18)),

            Make("Universitatea Craiova", "Craiova", "Mirel Rădoi", 78,
                new[] { stadiums[0] },
                ("Laurențiu Popescu", 30, "Goalkeeper", 12),
                ("Vladimir Screciu",  24, "Midfielder", 27),
                ("Nicușor Bancu",     32, "Defender",   3),
                ("Alexandru Mitriță", 30, "Forward",    11),
                ("Andrei Ivan",       28, "Forward",    7),
                ("Anzor Mekvabișvili",26, "Midfielder", 8),
                ("Bogdan Mitrea",     33, "Defender",   4),
                ("Asier Villalibre",  27, "Forward",    9)),

            Make("Petrolul Ploiești", "Ploiești", "Eugen Neagoe", 62,
                new[] { stadiums[3] },
                ("Bradley Mazikou",   25, "Defender",   3),
                ("Jair Tavares",      28, "Forward",    7),
                ("Andrei Pițian",     26, "Midfielder", 8),
                ("Constantin Grameni",24, "Midfielder", 10),
                ("Augusto Gentil",    27, "Forward",    11),
                ("Alexandru Crețu",   33, "Midfielder", 14),
                ("Tibor Tisza",       30, "Forward",    9),
                ("Petar Gigić",       31, "Defender",   5)),
        };
    }

    private static List<Match> BuildMatches(List<Team> teams, List<Stadium> stadiums)
    {
        // Round-robin-ish, partially played so the reports view has data.
        var fcsb = teams[0];
        var cfr = teams[1];
        var craiova = teams[2];
        var petrolul = teams[3];

        var today = DateTime.Today;

        return new()
        {
            new Match
            {
                Team1Key = fcsb.Key, Team2Key = cfr.Key,
                StadiumKey = stadiums[0].Key,
                Date = today.AddDays(-21), Score = "2:1",
            },
            new Match
            {
                Team1Key = craiova.Key, Team2Key = petrolul.Key,
                StadiumKey = stadiums[0].Key,
                Date = today.AddDays(-20), Score = "3:0",
            },
            new Match
            {
                Team1Key = fcsb.Key, Team2Key = craiova.Key,
                StadiumKey = stadiums[1].Key,
                Date = today.AddDays(-14), Score = "1:1",
            },
            new Match
            {
                Team1Key = cfr.Key, Team2Key = petrolul.Key,
                StadiumKey = stadiums[2].Key,
                Date = today.AddDays(-13), Score = "2:0",
            },
            new Match
            {
                Team1Key = petrolul.Key, Team2Key = fcsb.Key,
                StadiumKey = stadiums[3].Key,
                Date = today.AddDays(-7), Score = "0:2",
            },
            new Match
            {
                Team1Key = cfr.Key, Team2Key = craiova.Key,
                StadiumKey = stadiums[2].Key,
                Date = today.AddDays(7), Score = string.Empty,
            },
        };
    }
}
