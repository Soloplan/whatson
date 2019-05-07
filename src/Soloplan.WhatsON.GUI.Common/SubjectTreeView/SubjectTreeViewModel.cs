// <copyright file="SubjectTreeViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.SubjectTreeView
{
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.Linq;
  using System.Windows.Input;
  using NLog;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Top level viewmodel used to bind to <see cref="SubjectTreeView"/>.
  /// </summary>
  public class SubjectTreeViewModel : IHandleDoubleClick
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// Backing field for <see cref="SubjectGroups"/>.
    /// </summary>
    private ObservableCollection<SubjectGroupViewModel> subjectGroups;

    /// <summary>
    /// Gets observable collection of subject groups, the top level object in tree view binding.
    /// </summary>
    public ObservableCollection<SubjectGroupViewModel> SubjectGroups => this.subjectGroups ?? (this.subjectGroups = this.CreateSubjectGroupViewModelCollection());

    /// <summary>
    /// Gets first group from <see cref="SubjectGroups"/>, used for binding when there is just one group <see cref="OneGroup"/>.
    /// </summary>
    public SubjectGroupViewModel FirstGroup => this.SubjectGroups.FirstOrDefault();

    /// <summary>
    /// Gets a value indicating whether there is only one group.
    /// </summary>
    public bool OneGroup => this.SubjectGroups.Count == 1;

    /// <summary>
    /// Initializes the model.
    /// </summary>
    /// <param name="scheduler">Scheduler used for observation.</param>
    /// <param name="configuration">Configuration.</param>
    /// <param name="initialSubjectState">List of currently observed subjects - provide start data for model.</param>
    public void Init(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Subject> initialSubjectState)
    {
      log.Trace("Initializing {name}", nameof(SubjectTreeViewModel));
      this.Update(configuration);
      foreach (var subject in initialSubjectState)
      {
        log.Trace("Applying status for subject {@subject}", subject);
        this.SchedulerStatusQueried(this, subject);
      }

      scheduler.StatusQueried -= this.SchedulerStatusQueried;
      scheduler.StatusQueried += this.SchedulerStatusQueried;
    }

    /// <summary>
    /// Updates the model.
    /// </summary>
    /// <param name="configuration">Changed configuration.</param>
    public void Update(ApplicationConfiguration configuration)
    {
      log.Debug("Initializing {name}", nameof(SubjectTreeViewModel));
      var grouping = this.ParseConfiguration(configuration).ToList();
      foreach (var group in grouping)
      {
        log.Debug("Applying settings for group {group}", group.Key);
        var subjectGroupViewModel = this.SubjectGroups.FirstOrDefault(grp => grp.GroupName == group.Key);
        if (subjectGroupViewModel == null)
        {
          log.Debug("{model} doesn't exist, creating...", nameof(SubjectGroupViewModel));
          subjectGroupViewModel = new SubjectGroupViewModel();
          this.SubjectGroups.Add(subjectGroupViewModel);
        }

        subjectGroupViewModel.Init(group);
      }

      var noLongerAvailable = this.SubjectGroups.Where(grp => grouping.All(group => group.Key != grp.GroupName)).ToList();
      foreach (var subjectGroupViewModel in noLongerAvailable)
      {
        log.Debug("Removing group no longer present in configuration: {subjectGroupViewModelName}", subjectGroupViewModel.GroupName);
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

    /// <summary>
    /// Pares current expansion state of groups to <see cref="GroupExpansionSettings"/> list.
    /// </summary>
    /// <returns>List of <see cref="GroupExpansionSettings"/>.</returns>
    public IList<GroupExpansionSettings> GetGroupExpansionState()
    {
      return this.SubjectGroups.Select(group => new GroupExpansionSettings
      {
        GroupName = group.GroupName,
        Expanded = group.IsNodeExpanded,
      }).ToList();
    }

    /// <summary>
    /// Applies <see cref="GroupExpansionSettings"/> for groups in this model.
    /// </summary>
    /// <param name="groupExpansion">Group expansion settings.</param>
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

        log.Debug("Parsing expansion state for non-existing group {@GroupExpansion}", expansion);
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

      log.Warn("No viewmodel found for subject {@Subject}", e);
    }

    private ObservableCollection<SubjectGroupViewModel> CreateSubjectGroupViewModelCollection()
    {
      var subject = new ObservableCollection<SubjectGroupViewModel>();
      return subject;
    }
  }
}