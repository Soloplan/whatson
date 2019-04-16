namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.Linq;
  using System.Windows.Input;
  using Soloplan.WhatsON.GUI.VisualConfig;
  using Soloplan.WhatsON.Serialization;

  public class SubjectTreeViewModel : IHandleDoubleClick
  {
    private ObservableCollection<SubjectGroupViewModel> subjectGroups;

    public ObservableCollection<SubjectGroupViewModel> SubjectGroups => this.subjectGroups ?? (this.subjectGroups = this.CreateSubjectGroupViewModelCollection());

    public SubjectGroupViewModel FirstGroup => this.SubjectGroups.FirstOrDefault();

    public bool OneGroup => this.SubjectGroups.Count == 1;

    public void Init(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Subject> initialSubjectState)
    {
      this.Update(configuration);
      foreach (var subject in initialSubjectState)
      {
        this.SchedulerStatusQueried(this, subject);
      }

      scheduler.StatusQueried -= this.SchedulerStatusQueried;
      scheduler.StatusQueried += this.SchedulerStatusQueried;
    }

    public void Update(ApplicationConfiguration configuration)
    {
      var grouping = this.ParseConfiguration(configuration);
      foreach (var group in grouping)
      {
        var subjectGroupViewModel = this.SubjectGroups.FirstOrDefault(grp => grp.GroupName == group.Key);
        if (subjectGroupViewModel == null)
        {
          subjectGroupViewModel = new SubjectGroupViewModel();
          this.SubjectGroups.Add(subjectGroupViewModel);
        }

        subjectGroupViewModel.Init(group);
      }

      var noLongerAvailable = this.SubjectGroups.Where(grp => grouping.All(group => group.Key != grp.GroupName)).ToList();
      foreach (var subjectGroupViewModel in noLongerAvailable)
      {
        this.SubjectGroups.Remove(subjectGroupViewModel);
      }
    }

    public void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
      foreach (var subjectGroupViewModel in this.SubjectGroups)
      {
        subjectGroupViewModel.OnDoubleClick(sender, e);
      }
    }

    public IList<GroupExpansionSettings> GetGroupExpansionState()
    {
      return this.SubjectGroups.Select(group => new GroupExpansionSettings
      {
        GroupName = group.GroupName,
        Expanded = group.IsNodeExpanded,
      }).ToList();
    }

    public void ApplyGroupExpansionState(IList<GroupExpansionSettings> groupExpansion)
    {
      if (groupExpansion == null)
      {
        return;
      }

      foreach (var expansion in groupExpansion)
      {
        var targetGroup = this.SubjectGroups.FirstOrDefault(group => group.GroupName == expansion.GroupName);
        if (targetGroup != null)
        {
          targetGroup.IsNodeExpanded = expansion.Expanded;
        }
      }
    }

    private IEnumerable<IGrouping<string, SubjectConfiguration>> ParseConfiguration(ApplicationConfiguration configuration)
    {
      return configuration.SubjectsConfiguration.GroupBy(config => config.GetConfigurationByKey(Subject.Category)?.Value?.Trim() ?? string.Empty);
    }

    private void SchedulerStatusQueried(object sender, Subject e)
    {
      foreach (var subjectGroupViewModel in this.SubjectGroups)
      {
        if (subjectGroupViewModel.Update(e))
        {
          return;
        }
      }
    }

    private ObservableCollection<SubjectGroupViewModel> CreateSubjectGroupViewModelCollection()
    {
      var subject = new ObservableCollection<SubjectGroupViewModel>();
      return subject;
    }
  }
}