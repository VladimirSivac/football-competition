using CommunityToolkit.Mvvm.ComponentModel;

namespace FootballCompetition.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Optional human-friendly label used by the navigation sidebar.
    /// </summary>
    public virtual string DisplayName => GetType().Name;
}
