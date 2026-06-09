using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FootballCompetition.Models;
using FootballCompetition.Services;

namespace FootballCompetition.ViewModels;

public partial class MatchesViewModel : ViewModelBase, IRefreshable
{
    private readonly FootballManagerService _service;
    private readonly Action<Exception> _reportError;

    public MatchesViewModel(FootballManagerService service, Action<Exception> reportError)
    {
        _service = service;
        _reportError = reportError;
        Refresh();
    }

    public ObservableCollection<MatchRow> Matches { get; } = new();
    public ObservableCollection<Team> Teams { get; } = new();
    public ObservableCollection<Stadium> Stadiums { get; } = new();

    [ObservableProperty]
    private MatchRow? _selectedMatch;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private Match _draft = NewMatch();

    [ObservableProperty]
    private Team? _draftTeam1;

    [ObservableProperty]
    private Team? _draftTeam2;

    [ObservableProperty]
    private Stadium? _draftStadium;

    [ObservableProperty]
    private DateTime _draftDate = DateTime.Today;

    public void Refresh()
    {
        try
        {
            var teams = _service.GetTeams().OrderBy(t => t.Name).ToList();
            var stadiums = _service.GetStadiums().OrderBy(s => s.Name).ToList();
            var teamsByKey = teams.ToDictionary(t => t.Key, t => t);
            var stadiumsByKey = stadiums.ToDictionary(s => s.Key, s => s);

            Teams.Clear();
            foreach (var t in teams) Teams.Add(t);

            Stadiums.Clear();
            foreach (var s in stadiums) Stadiums.Add(s);

            Matches.Clear();
            foreach (var m in _service.GetMatches().OrderByDescending(m => m.Date))
            {
                Matches.Add(new MatchRow(
                    m,
                    teamsByKey.TryGetValue(m.Team1Key, out var t1) ? t1.Name : "(?)",
                    teamsByKey.TryGetValue(m.Team2Key, out var t2) ? t2.Name : "(?)",
                    stadiumsByKey.TryGetValue(m.StadiumKey, out var st) ? st.Name : "(?)"));
            }

            CancelEdit();
        }
        catch (Exception ex)
        {
            _reportError(ex);
        }
    }

    [RelayCommand]
    private void New()
    {
        Draft = NewMatch();
        DraftTeam1 = null;
        DraftTeam2 = null;
        DraftStadium = null;
        DraftDate = DateTime.Today;
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedMatch is null) return;
        var src = SelectedMatch.Match;
        Draft = Clone(src);
        DraftTeam1 = Teams.FirstOrDefault(t => t.Key == src.Team1Key);
        DraftTeam2 = Teams.FirstOrDefault(t => t.Key == src.Team2Key);
        DraftStadium = Stadiums.FirstOrDefault(s => s.Key == src.StadiumKey);
        DraftDate = src.Date;
        IsEditing = true;
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            if (DraftTeam1 is null || DraftTeam2 is null)
            {
                throw new InvalidOperationException("Должны быть выбраны обе команды.");
            }
            if (DraftTeam1.Key == DraftTeam2.Key)
            {
                throw new InvalidOperationException("Команда не может играть сама с собой.");
            }
            if (DraftStadium is null)
            {
                throw new InvalidOperationException("Должен быть выбран стадион.");
            }
            if (!string.IsNullOrWhiteSpace(Draft.Score) &&
                !FootballManagerService.TryParseScore(Draft.Score, out _, out _))
            {
                throw new InvalidOperationException(
                    "Счет должен быть в формате 'хозяева:гости' (например, \"2:1\") или оставлен пустым.");
            }

            Draft.Team1Key = DraftTeam1.Key;
            Draft.Team2Key = DraftTeam2.Key;
            Draft.StadiumKey = DraftStadium.Key;
            Draft.Date = DraftDate;

            var existing = _service.GetMatches().FirstOrDefault(m => m.Key == Draft.Key);
            if (existing is null)
            {
                _service.AddMatch(Draft);
            }
            else
            {
                _service.UpdateMatch(Draft);
            }
            Refresh();
        }
        catch (Exception ex)
        {
            _reportError(ex);
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedMatch is null) return;
        try
        {
            _service.DeleteMatch(SelectedMatch.Match);
            Refresh();
        }
        catch (Exception ex)
        {
            _reportError(ex);
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        Draft = NewMatch();
    }

    private static Match NewMatch() => new()
    {
        Date = DateTime.Today,
        Score = string.Empty,
    };

    private static Match Clone(Match m) => new()
    {
        Key = m.Key,
        Team1Key = m.Team1Key,
        Team2Key = m.Team2Key,
        StadiumKey = m.StadiumKey,
        Date = m.Date,
        Score = m.Score,
    };

    public record MatchRow(Match Match, string Team1Name, string Team2Name, string StadiumName)
    {
        public DateTime Date => Match.Date;
        public string Score => string.IsNullOrWhiteSpace(Match.Score) ? "—" : Match.Score;
    }
}
