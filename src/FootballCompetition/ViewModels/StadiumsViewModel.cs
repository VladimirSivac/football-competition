using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FootballCompetition.Models;
using FootballCompetition.Services;

namespace FootballCompetition.ViewModels;

public partial class StadiumsViewModel : ViewModelBase, IRefreshable
{
    private readonly FootballManagerService _service;
    private readonly Action<Exception> _reportError;

    public StadiumsViewModel(FootballManagerService service, Action<Exception> reportError)
    {
        _service = service;
        _reportError = reportError;
        Refresh();
    }

    public ObservableCollection<Stadium> Stadiums { get; } = new();

    [ObservableProperty]
    private Stadium? _selectedStadium;

    [ObservableProperty]
    private Stadium _draft = NewStadium();

    [ObservableProperty]
    private bool _isEditing;

    public void Refresh()
    {
        try
        {
            Stadiums.Clear();
            foreach (var s in _service.GetStadiums().OrderBy(s => s.Name))
            {
                Stadiums.Add(s);
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
        Draft = NewStadium();
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedStadium is null) return;
        Draft = Clone(SelectedStadium);
        IsEditing = true;
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Draft.Name))
            {
                throw new InvalidOperationException("Stadium name is required.");
            }
            if (Draft.Capacity < 0)
            {
                throw new InvalidOperationException("Capacity must be non-negative.");
            }
            if (Draft.TicketPrice < 0)
            {
                throw new InvalidOperationException("Ticket price must be non-negative.");
            }

            var existing = _service.GetStadiums().FirstOrDefault(s => s.Key == Draft.Key);
            if (existing is null)
            {
                _service.AddStadium(Draft);
            }
            else
            {
                _service.UpdateStadium(Draft);
            }
            // Capture the saved key before Refresh() clears Draft via CancelEdit().
            var savedKey = Draft.Key;
            Refresh();
            SelectedStadium = Stadiums.FirstOrDefault(s => s.Key == savedKey);
        }
        catch (Exception ex)
        {
            _reportError(ex);
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedStadium is null) return;
        try
        {
            _service.DeleteStadium(SelectedStadium);
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
        Draft = NewStadium();
    }

    private static Stadium NewStadium() => new()
    {
        Name = string.Empty,
        City = string.Empty,
        Capacity = 0,
        TicketPrice = 0,
    };

    private static Stadium Clone(Stadium s) => new()
    {
        Key = s.Key,
        Name = s.Name,
        City = s.City,
        Capacity = s.Capacity,
        TicketPrice = s.TicketPrice,
    };
}
