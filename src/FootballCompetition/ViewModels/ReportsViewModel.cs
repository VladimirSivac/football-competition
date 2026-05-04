using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FootballCompetition.Models;
using FootballCompetition.Services;

namespace FootballCompetition.ViewModels;

public partial class ReportsViewModel : ViewModelBase, IRefreshable
{
    private readonly FootballManagerService _service;
    private readonly Action<Exception> _reportError;

    public ReportsViewModel(FootballManagerService service, Action<Exception> reportError)
    {
        _service = service;
        _reportError = reportError;
        Refresh();
    }

    public ObservableCollection<Team> Teams { get; } = new();
    public ObservableCollection<Stadium> Stadiums { get; } = new();

    // Q1
    [ObservableProperty]
    private Team? _q1Team;
    public ObservableCollection<TeamMatchSummary> Q1Results { get; } = new();

    // Q2
    [ObservableProperty]
    private DateTime _q2Date = DateTime.Today;
    [ObservableProperty]
    private Stadium? _q2Stadium;
    public ObservableCollection<PlayerLineupRow> Q2Results { get; } = new();

    // Q3
    [ObservableProperty]
    private Team? _q3Team1;
    [ObservableProperty]
    private Team? _q3Team2;
    [ObservableProperty]
    private string _q3Result = "(not calculated)";

    // Q4
    public ObservableCollection<TeamGoalDifference> Q4Results { get; } = new();
    [ObservableProperty]
    private string _q4Headline = "(not calculated)";

    // Q5
    public ObservableCollection<TeamStandingRow> Q5Results { get; } = new();

    // Q6
    public ObservableCollection<StadiumScheduleGroup> Q6Results { get; } = new();

    public void Refresh()
    {
        try
        {
            Teams.Clear();
            foreach (var t in _service.GetTeams().OrderBy(t => t.Name)) Teams.Add(t);

            Stadiums.Clear();
            foreach (var s in _service.GetStadiums().OrderBy(s => s.Name)) Stadiums.Add(s);
        }
        catch (Exception ex)
        {
            _reportError(ex);
        }
    }

    [RelayCommand]
    private void RunQ1()
    {
        try
        {
            Q1Results.Clear();
            if (Q1Team is null) return;
            foreach (var row in _service.GetMatchesForTeam(Q1Team.Key))
            {
                Q1Results.Add(row);
            }
        }
        catch (Exception ex) { _reportError(ex); }
    }

    [RelayCommand]
    private void RunQ2()
    {
        try
        {
            Q2Results.Clear();
            if (Q2Stadium is null) return;
            foreach (var row in _service.GetPlayersInMatchOnDateAtStadium(Q2Date, Q2Stadium.Key))
            {
                Q2Results.Add(row);
            }
        }
        catch (Exception ex) { _reportError(ex); }
    }

    [RelayCommand]
    private void RunQ3()
    {
        try
        {
            if (Q3Team1 is null || Q3Team2 is null)
            {
                Q3Result = "Pick both teams.";
                return;
            }
            if (Q3Team1.Key == Q3Team2.Key)
            {
                Q3Result = "A team cannot play itself.";
                return;
            }
            var price = _service.GetTicketPriceForTeams(Q3Team1.Key, Q3Team2.Key);
            Q3Result = $"{price:0.00} for {Q3Team1.Name} vs {Q3Team2.Name}";
        }
        catch (Exception ex) { _reportError(ex); }
    }

    [RelayCommand]
    private void RunQ4()
    {
        try
        {
            var report = _service.GetBestAndWorstGoalDifference();
            Q4Results.Clear();
            foreach (var row in report.All) Q4Results.Add(row);
            Q4Headline = $"Best: {report.Best.Team} ({report.Best.GoalDifference:+#;-#;0})  •  Worst: {report.Worst.Team} ({report.Worst.GoalDifference:+#;-#;0})";
        }
        catch (Exception ex) { _reportError(ex); }
    }

    [RelayCommand]
    private void RunQ5()
    {
        try
        {
            Q5Results.Clear();
            foreach (var row in _service.GetPrizeWinningTeams()) Q5Results.Add(row);
        }
        catch (Exception ex) { _reportError(ex); }
    }

    [RelayCommand]
    private void RunQ6()
    {
        try
        {
            Q6Results.Clear();
            foreach (var grp in _service.GetScheduleByStadium()) Q6Results.Add(grp);
        }
        catch (Exception ex) { _reportError(ex); }
    }
}
