// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsStatusViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.GUI
{
  using System;
  using System.Collections.ObjectModel;
  using System.Linq;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  public class JenkinsStatusViewModel : BuildStatusViewModel
  {
    private ObservableCollection<UserViewModel> committedToThisBuild;

    private bool culpritsAndLastCommittedDifferent;

    public JenkinsStatusViewModel(ConnectorViewModel model)
      : base(model)
    {
    }

    public bool CulpritsAndLastCommittedDifferent
    {
      get => this.culpritsAndLastCommittedDifferent;
      set
      {
        if (this.culpritsAndLastCommittedDifferent != value)
        {
          this.culpritsAndLastCommittedDifferent = value;
          this.OnPropertyChanged();
        }
      }
    }

    public ObservableCollection<UserViewModel> CommittedToThisBuild => this.committedToThisBuild ?? (this.committedToThisBuild = new ObservableCollection<UserViewModel>());

    public override void Update(Status newStatus)
    {
      base.Update(newStatus);
      if (!(newStatus is JenkinsStatus jenkinsStatus))
      {
        return;
      }

      this.DisplayName = jenkinsStatus.DisplayName;

      // convert the UTC time to local time for displaying purposes
      this.Time = this.Time.ToLocalTime();

      if (this.State == ObservationState.Running)
      {
        var elapsedSinceStart = (DateTime.Now - this.Time).TotalSeconds;
        this.RawProgress = (int)((100 * elapsedSinceStart) / this.EstimatedDuration.TotalSeconds);
        this.Duration = DateTime.Now - this.Time;
      }
      else
      {
        this.RawProgress = 0;
      }

      this.CommittedToThisBuild.Clear();
      this.Culprits.Clear();
      foreach (var culprit in jenkinsStatus.Culprits)
      {
        var culpritModel = new UserViewModel() { FullName = culprit.FullName, Url = culprit.AbsoluteUrl };
        this.Culprits.Add(culpritModel);
      }

      foreach (var culprit in jenkinsStatus.CommittedToThisBuild ?? Enumerable.Empty<JenkinsUser>())
      {
        var culpritModel = new UserViewModel() { FullName = culprit.FullName, Url = culprit.AbsoluteUrl };
        this.CommittedToThisBuild.Add(culpritModel);
      }

      this.CulpritsAndLastCommittedDifferent = this.Culprits.Count != this.CommittedToThisBuild.Count || this.Culprits.Any(culprit => this.CommittedToThisBuild.All(committer => committer.FullName != culprit.FullName));

      this.UpdateCalculatedFields();
    }
  }
}
