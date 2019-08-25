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
  using Soloplan.WhatsON.GUI.Common;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  public class JenkinsStatusViewModel : BuildStatusViewModel
  {
    private OpenJenkinsWebPageCommandData parentCommandData;

    private ObservableCollection<CulpritViewModel> committedToThisBuild;

    private bool culpritsAndLastCommittedDifferent;

    public JenkinsStatusViewModel(JenkinsProjectViewModel model)
      : base(model)
    {
    }

    public override OpenWebPageCommandData OpenBuildPageCommandData
    {
      get
      {
        if (this.parentCommandData == null)
        {
          return null;
        }

        return new OpenJenkinsWebPageCommandData
        {
          Address = this.parentCommandData.Address + "/" + this.BuildNumber,
          Redirect = this.parentCommandData.Redirect,
        };
      }
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

    public ObservableCollection<CulpritViewModel> CommittedToThisBuild => this.committedToThisBuild ?? (this.committedToThisBuild = new ObservableCollection<CulpritViewModel>());

    public override void Update(Status newStatus)
    {
      base.Update(newStatus);
      if (!(newStatus is JenkinsStatus jenkinsStatus))
      {
        this.BuildNumber = null;
        return;
      }

      this.BuildNumber = jenkinsStatus.BuildNumber;
      this.DisplayName = jenkinsStatus.DisplayName;
      this.Building = jenkinsStatus.Building;
      this.Duration = jenkinsStatus.Duration;
      this.EstimatedDuration = jenkinsStatus.EstimatedDuration;

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
        var culpritModel = new CulpritViewModel() { FullName = culprit.FullName, Url = culprit.AbsoluteUrl };
        this.Culprits.Add(culpritModel);
      }

      foreach (var culprit in jenkinsStatus.CommittedToThisBuild ?? Enumerable.Empty<Culprit>())
      {
        var culpritModel = new CulpritViewModel() { FullName = culprit.FullName, Url = culprit.AbsoluteUrl };
        this.CommittedToThisBuild.Add(culpritModel);
      }

      this.CulpritsAndLastCommittedDifferent = this.Culprits.Count != this.CommittedToThisBuild.Count || this.Culprits.Any(culprit => this.CommittedToThisBuild.All(committer => committer.FullName != culprit.FullName));

      this.UpdateCalculatedFields();
    }

    public void SetJobAddress(OpenJenkinsWebPageCommandData parentData)
    {
      this.parentCommandData = parentData;
      this.OnPropertyChanged(nameof(this.OpenBuildPageCommandData));
    }
  }
}
