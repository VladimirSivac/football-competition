using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FootballCompetition.Models;
using FootballCompetition.Services;

namespace FootballCompetition.ViewModels;

public partial class TeamsViewModel : ViewModelBase, IRefreshable
{
    private readonly FootballManagerService _service;
    private readonly Action<Exception> _reportError;

    public TeamsViewModel(FootballManagerService service, Action<Exception> reportError)
    {
        _service = service;
        _reportError = reportError;
        Refresh();
    }

    public ObservableCollection<Team> Teams { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(SelectedTeamPlayers))]
    private Team? _selectedTeam;

    [ObservableProperty]
    private Team _draft = NewTeam();

    [ObservableProperty]
    private bool _isEditing;

    public ObservableCollection<Player> SelectedTeamPlayers
    {
        get
        {
            var col = new ObservableCollection<Player>();
            if (SelectedTeam is { } t)
            {
                foreach (var p in t.Players) col.Add(p);
            }
            return col;
        }
    }

    public void Refresh()
    {
        try
        {
            Teams.Clear();
            foreach (var t in _service.GetTeams().OrderBy(t => t.Name))
            {
                Teams.Add(t);
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
        Draft = NewTeam();
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedTeam is null) return;
        Draft = Clone(SelectedTeam);
        IsEditing = true;
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Draft.Name))
            {
                throw new InvalidOperationException("Название команды обязательно к заполнению.");
            }

            var existing = _service.GetTeams().FirstOrDefault(t => t.Key == Draft.Key);
            if (existing is null)
            {
                _service.AddTeam(Draft);
            }
            else
            {
                _service.UpdateTeam(Draft);
            }
            // Capture the saved key before Refresh() clears Draft via CancelEdit().
            var savedKey = Draft.Key;
            Refresh();
            SelectedTeam = Teams.FirstOrDefault(t => t.Key == savedKey);
        }
        catch (Exception ex)
        {
            _reportError(ex);
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedTeam is null) return;
        try
        {
            _service.DeleteTeam(SelectedTeam);
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
        Draft = NewTeam();
    }

    private static Team NewTeam() => new()
    {
        Name = string.Empty,
        City = string.Empty,
        Coach = string.Empty,
        LastSeasonRating = 50,
    };

    private static Team Clone(Team source) => new()
    {
        Key = source.Key,
        Name = source.Name,
        City = source.City,
        Coach = source.Coach,
        LastSeasonRating = source.LastSeasonRating,
        Players = source.Players.ToList(),
        Stadiums = source.Stadiums.ToList(),
    };
}
