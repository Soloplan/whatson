// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="SubjectTreeViewModel.cs" company="Soloplan GmbH">
// //   Copyright (c) Soloplan GmbH. All rights reserved.
// // </copyright>
// // --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System;
  using System.Collections.ObjectModel;
  using System.Linq;

  public class SubjectTreeViewModel
  {
    private object lockObject = new object();

    private ObservationScheduler scheduler;

    private ObservableCollection<SubjectGroupViewModel> subjectGroups;

    public event EventHandler CountChanged;

    public ObservableCollection<SubjectGroupViewModel> SubjectGroups => this.subjectGroups ?? (this.subjectGroups = this.CreateSubjectGroupViewModelCollection());

    private ObservableCollection<SubjectGroupViewModel> CreateSubjectGroupViewModelCollection()
    {
      var subject = new ObservableCollection<SubjectGroupViewModel>();
      subject.CollectionChanged += this.OnSubjectCollectionChanged;

      return subject;
    }

    private void OnSubjectCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
      {
        this.CountChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    public SubjectGroupViewModel FirstGroup => this.SubjectGroups.FirstOrDefault();

    public bool OneGroup => this.SubjectGroups.Count == 1;

    public void Init(ObservationScheduler scheduler, Configuration configuration)
    {
      var subjectGroup = new SubjectGroupViewModel();
      this.scheduler = scheduler;
      this.scheduler.StatusQueried -= this.SchedulerStatusQueried;
      this.scheduler.StatusQueried += this.SchedulerStatusQueried;
      this.ParseConfiguration(configuration);
    }

    private void ParseConfiguration(Configuration configuration)
    {
      var grouping = configuration.Subjects.GroupBy(config => config.GetConfigurationByKey(Subject.Category)?.Value);
      foreach (var group in grouping)
      {
        var subjectGroupViewModel = new SubjectGroupViewModel();
        subjectGroupViewModel.Init(group);
        this.SubjectGroups.Add(subjectGroupViewModel);
      }
    }

    private void SchedulerStatusQueried(object sender, Subject e)
    {
      lock (this.lockObject)
      {
        foreach (var subjectGroupViewModel in this.SubjectGroups)
        {
          if (subjectGroupViewModel.Update(e))
          {
            return;
          }
        }
      }
    }

  }
}