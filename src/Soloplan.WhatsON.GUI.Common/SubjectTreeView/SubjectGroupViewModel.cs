// <copyright file="SubjectGroupViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.SubjectTreeView
{
  using System.Collections.ObjectModel;
  using System.Linq;
  using System.Windows.Input;
  using NLog;

  /// <summary>
  /// Viewmodel representing group of subjects shown as single node in <see cref="SubjectTreeView"/>.
  /// </summary>
  public class SubjectGroupViewModel : TreeItemViewModel
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    private string groupName;

    ObservableCollection<SubjectViewModel> statusViewModels;

    public SubjectGroupViewModel()
    {
      this.IsNodeExpanded = true;
    }

    public ObservableCollection<SubjectViewModel> SubjectViewModels => this.statusViewModels ?? (this.statusViewModels = new ObservableCollection<SubjectViewModel>());

    public string GroupName
    {
      get => this.groupName;
      set
      {
        if (this.groupName != value)
        {
          this.groupName = value;
          this.OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Called when subject changes, ex new status.
    /// </summary>
    /// <param name="changedSubject">The subject which changed.</param>
    /// <returns>True if something was changed; false otherwise.</returns>
    public bool Update(Subject changedSubject)
    {
      var changedViewModel = this.SubjectViewModels.FirstOrDefault(subject => subject.Identifier == changedSubject.SubjectConfiguration.Identifier);
      if (changedViewModel != null)
      {
        changedViewModel.Update(changedSubject);
        return true;
      }

      return false;
    }

    /// <summary>
    /// Should be called when configuration changed.
    /// </summary>
    /// <param name="subjectGroup">Grouping of subjects by group name.</param>
    public void Init(IGrouping<string, SubjectConfiguration> subjectGroup)
    {
      this.GroupName = subjectGroup.Key ?? string.Empty;
      log.Debug("Initializing group {GroupName}", this.GroupName);

      var subjectsNoLongerPresent = this.SubjectViewModels.Where(model => subjectGroup.All(configurationSubject => configurationSubject.Identifier != model.Identifier)).ToList();
      var newSubjects = subjectGroup.Where(configurationSubject => this.SubjectViewModels.All(viewModel => configurationSubject.Identifier != viewModel.Identifier));
      foreach (var noLongerPresentSubjectViewModel in subjectsNoLongerPresent)
      {
        log.Debug("Remove no longer present subject {noLongerPresentSubjectViewModel}", new { Identifier = noLongerPresentSubjectViewModel.Identifier, Name = noLongerPresentSubjectViewModel.Name });
        this.SubjectViewModels.Remove(noLongerPresentSubjectViewModel);
      }

      foreach (var subjectViewModel in this.SubjectViewModels)
      {
        log.Debug("Updating viewmodel for {subjectConfiguration}", new { Identifier = subjectViewModel.Identifier, Name = subjectViewModel.Name });
        var config = subjectGroup.FirstOrDefault(configurationSubject => subjectViewModel.Identifier == configurationSubject.Identifier);
        subjectViewModel.Init(config);
      }

      foreach (var newSubject in newSubjects)
      {
        log.Debug("Adding new subject {subjectConfiguration}", new { Identifier = newSubject.Identifier, Name = newSubject.Name });
        this.CreateViewModelForSubjectConfiguration(newSubject);
      }
    }

    private void CreateViewModelForSubjectConfiguration(SubjectConfiguration subjectConfiguration)
    {
      var subject = PluginsManager.Instance.GetSubject(subjectConfiguration);
      SubjectViewModel subjectViewModel = this.GetSubjectViewModel(subject);
      subjectViewModel.Init(subjectConfiguration);
      subjectViewModel.Update(subject);
      this.SubjectViewModels.Add(subjectViewModel);
    }

    public override void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
      foreach (var subjectViewModel in this.SubjectViewModels)
      {
        subjectViewModel.OnDoubleClick(sender, e);
      }
    }

    private SubjectViewModel GetSubjectViewModel(Subject subject)
    {
      if (subject == null)
      {
        return new SubjectMissingViewModel();
      }

      var presentationPlugIn = PluginsManager.Instance.GetPresentationPlugIn(subject.GetType());
      if (presentationPlugIn != null)
      {
        return presentationPlugIn.CreateViewModel();
      }

      return new SubjectViewModel();
    }
  }
}