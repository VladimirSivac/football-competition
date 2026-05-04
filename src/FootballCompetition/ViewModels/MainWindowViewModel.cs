using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FootballCompetition.Services;

namespace FootballCompetition.ViewModels;

/// <summary>
/// Orchestrates section navigation and surfaces the global error overlay.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly FootballManagerService _service;

    public MainWindowViewModel() : this(DesignTime.Service)
    {
    }

    public MainWindowViewModel(FootballManagerService service)
    {
        _service = service;

        Teams = new TeamsViewModel(service, ReportError);
        Players = new PlayersViewModel(service, ReportError);
        Stadiums = new StadiumsViewModel(service, ReportError);
        Matches = new MatchesViewModel(service, ReportError);
        Reports = new ReportsViewModel(service, ReportError);

        Sections = new ObservableCollection<NavigationSection>
        {
            new("Teams",    "\uE716", Teams),
            new("Players",  "\uE13D", Players),
            new("Stadiums", "\uE707", Stadiums),
            new("Matches",  "\uE787", Matches),
            new("Reports",  "\uE9D9", Reports),
        };

        SelectedSection = Sections[0];
    }

    [ObservableProperty]
    private NavigationSection? _selectedSection;

    public ObservableCollection<NavigationSection> Sections { get; }

    public TeamsViewModel Teams { get; }
    public PlayersViewModel Players { get; }
    public StadiumsViewModel Stadiums { get; }
    public MatchesViewModel Matches { get; }
    public ReportsViewModel Reports { get; }

    /// <summary>
    /// True while a non-empty <see cref="ErrorMessage"/> is active. The view
    /// uses this to fade in the modal error overlay.
    /// </summary>
    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _errorTitle = "Something went wrong";

    [RelayCommand]
    private void DismissError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Centralised error sink. Any ViewModel can call this to surface a
    /// failure to the user. We intentionally swallow nothing — by routing
    /// every error through this one path we keep the UX consistent.
    /// </summary>
    public void ReportError(Exception ex)
    {
        ErrorTitle = ex.GetType().Name;
        ErrorMessage = ex.Message;
        HasError = true;
    }

    partial void OnSelectedSectionChanged(NavigationSection? value)
    {
        // Each section refreshes itself when activated so the data the user
        // sees is never stale relative to mutations made elsewhere.
        if (value?.ViewModel is IRefreshable refreshable)
        {
            try
            {
                refreshable.Refresh();
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }
    }

    /// <summary>
    /// Lightweight DTO for the sidebar list. Glyph is a Segoe Fluent Icons
    /// codepoint; on platforms without that font Avalonia just renders
    /// nothing, which is acceptable.
    /// </summary>
    public record NavigationSection(string Title, string Glyph, ViewModelBase ViewModel);

    private static class DesignTime
    {
        public static readonly FootballManagerService Service = BuildDesignService();

        private static FootballManagerService BuildDesignService()
        {
            // Design-time: in-memory, never touches disk.
            var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fc-design");
            System.IO.Directory.CreateDirectory(dir);
            var teams = new Data.FileRepository<Models.Team>(System.IO.Path.Combine(dir, "teams.json"));
            var players = new Data.FileRepository<Models.Player>(System.IO.Path.Combine(dir, "players.json"));
            var stadiums = new Data.FileRepository<Models.Stadium>(System.IO.Path.Combine(dir, "stadiums.json"));
            var matches = new Data.FileRepository<Models.Match>(System.IO.Path.Combine(dir, "matches.json"));
            return new FootballManagerService(teams, players, stadiums, matches);
        }
    }
}

/// <summary>
/// Implemented by section ViewModels so the shell can ask them to reload
/// after navigation.
/// </summary>
public interface IRefreshable
{
    void Refresh();
}
