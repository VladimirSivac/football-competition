using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FootballCompetition.Models;
using FootballCompetition.Services;

namespace FootballCompetition.ViewModels;

public partial class PlayersViewModel : ViewModelBase, IRefreshable
{
    private readonly FootballManagerService _service;
    private readonly Action<Exception> _reportError;

    public PlayersViewModel(FootballManagerService service, Action<Exception> reportError)
    {
        _service = service;
        _reportError = reportError;
        Refresh();
    }

    public ObservableCollection<Team> Teams { get; } = new();
    public ObservableCollection<Player> Players { get; } = new();

    public string[] Roles { get; } =
    {
        "Вратарь", "Защитник", "Полузащитник", "Нападающий",
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditOrDelete))]
    private Player? _selectedPlayer;

    [ObservableProperty]
    private Team? _selectedTeam;

    [ObservableProperty]
    private Player _draft = NewPlayer();

    [ObservableProperty]
    private bool _isEditing;

    public bool CanEditOrDelete => SelectedPlayer is not null;

    public void Refresh()
    {
        try
        {
            Teams.Clear();
            foreach (var t in _service.GetTeams().OrderBy(t => t.Name))
            {
                Teams.Add(t);
            }

            Players.Clear();
            foreach (var p in _service.GetPlayers().OrderBy(p => p.FullName))
            {
                Players.Add(p);
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
        Draft = NewPlayer();
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedPlayer is null) return;
        Draft = Clone(SelectedPlayer);
        SelectedTeam = _service.GetTeamForPlayer(SelectedPlayer.Key);
        IsEditing = true;
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            if (SelectedTeam is null)
            {
                throw new InvalidOperationException("Выберите команду для игрока.");
            }
            if (string.IsNullOrWhiteSpace(Draft.FullName))
            {
                throw new InvalidOperationException("ФИО игрока обязательно к заполнению.");
            }

            var existing = _service.GetPlayers().FirstOrDefault(p => p.Key == Draft.Key);
            if (existing is null)
            {
                _service.AddPlayer(Draft, SelectedTeam);
            }
            else
            {
                // If the player moved teams, remove from old then add to new.
                var oldTeam = _service.GetTeamForPlayer(Draft.Key);
                if (oldTeam is not null && oldTeam.Key != SelectedTeam.Key)
                {
                    _service.DeletePlayer(existing, oldTeam);
                    _service.AddPlayer(Draft, SelectedTeam);
                }
                else
                {
                    _service.UpdatePlayer(Draft, SelectedTeam);
                }
            }

            // Capture the saved key before Refresh() clears Draft via CancelEdit().
            var savedKey = Draft.Key;
            Refresh();
            SelectedPlayer = Players.FirstOrDefault(p => p.Key == savedKey);
        }
        catch (Exception ex)
        {
            _reportError(ex);
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedPlayer is null) return;
        try
        {
            var team = _service.GetTeamForPlayer(SelectedPlayer.Key);
            _service.DeletePlayer(SelectedPlayer, team);
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
        Draft = NewPlayer();
        SelectedTeam = null;
    }

    partial void OnSelectedPlayerChanged(Player? value)
    {
        if (value is not null)
        {
            SelectedTeam = _service.GetTeamForPlayer(value.Key);
        }
    }

    private static Player NewPlayer() => new()
    {
        FullName = string.Empty,
        Age = 18,
        Role = "Полузащитник",
        Number = 0,
    };

    private static Player Clone(Player source) => new()
    {
        Key = source.Key,
        FullName = source.FullName,
        Age = source.Age,
        Role = source.Role,
        Number = source.Number,
    };
}
