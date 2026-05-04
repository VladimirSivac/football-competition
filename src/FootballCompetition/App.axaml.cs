using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FootballCompetition.Data;
using FootballCompetition.Models;
using FootballCompetition.Services;
using FootballCompetition.ViewModels;
using FootballCompetition.Views;

namespace FootballCompetition;

public partial class App : Application
{
    /// <summary>
    /// Where the JSON store lives at runtime. Exposed so tests / the user
    /// can locate the files without hunting around.
    /// </summary>
    public static string DataDirectory { get; } = ResolveDataDirectory();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                Directory.CreateDirectory(DataDirectory);

                var teams = new FileRepository<Team>(Path.Combine(DataDirectory, "teams.json"));
                var players = new FileRepository<Player>(Path.Combine(DataDirectory, "players.json"));
                var stadiums = new FileRepository<Stadium>(Path.Combine(DataDirectory, "stadiums.json"));
                var matches = new FileRepository<Match>(Path.Combine(DataDirectory, "matches.json"));

                DataSeeder.SeedIfEmpty(DataDirectory, teams, players, stadiums, matches);

                var service = new FootballManagerService(teams, players, stadiums, matches);
                var vm = new MainWindowViewModel(service);
                desktop.MainWindow = new MainWindow { DataContext = vm };
            }
            catch (Exception ex)
            {
                // Bootstrap-time failure (e.g. corrupt JSON, locked file): the
                // main window is the only place we have to show errors, so
                // create one with an empty service and surface the failure.
                var fallback = new FallbackBootstrap(ex);
                desktop.MainWindow = new MainWindow { DataContext = fallback.ViewModel };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string ResolveDataDirectory()
    {
        // Allow overriding via env var so test runs / CI can use a temp dir.
        var fromEnv = Environment.GetEnvironmentVariable("FOOTBALL_COMPETITION_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(appData))
        {
            appData = Path.Combine(Path.GetTempPath(), "FootballCompetition");
        }
        return Path.Combine(appData, "FootballCompetition", "data");
    }

    private sealed class FallbackBootstrap
    {
        public MainWindowViewModel ViewModel { get; }

        public FallbackBootstrap(Exception bootError)
        {
            // Build an in-memory service so the UI can still navigate; the
            // user immediately sees the error overlay describing what failed.
            var dir = Path.Combine(Path.GetTempPath(), "fc-fallback-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var teams = new FileRepository<Team>(Path.Combine(dir, "teams.json"));
            var players = new FileRepository<Player>(Path.Combine(dir, "players.json"));
            var stadiums = new FileRepository<Stadium>(Path.Combine(dir, "stadiums.json"));
            var matches = new FileRepository<Match>(Path.Combine(dir, "matches.json"));
            var service = new FootballManagerService(teams, players, stadiums, matches);
            ViewModel = new MainWindowViewModel(service);
            ViewModel.ReportError(bootError);
        }
    }
}
