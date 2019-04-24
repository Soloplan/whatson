// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlStatusViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.GUI
{
  using System;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.SubjectTreeView;
  using Soloplan.WhatsON.Jenkins.GUI;

  public class CruiseControlStatusViewModel : BuildStatusViewModel
  {
    private OpenWebPageCommandData openBuildPageCommandData;

    public CruiseControlStatusViewModel(SubjectViewModel model)
      : base(model)
    {
    }

    public override OpenWebPageCommandData OpenBuildPageCommandData => this.openBuildPageCommandData;

    public override void Update(Status newStatus)
    {
      base.Update(newStatus);
      var ccStatus = newStatus as CruiseControlStatus;
      if (ccStatus == null)
      {
        this.BuildNumber = null;
        return;
      }

      this.BuildNumber = ccStatus.BuildNumber;
      this.Building = ccStatus.Building;
      this.Duration = ccStatus.Duration;
      this.EstimatedDuration = ccStatus.EstimatedDuration;

      if (this.State == ObservationState.Running && this.EstimatedDuration.TotalSeconds > 0)
      {
        var elapsedSinceStart = (DateTime.Now - this.Time).TotalSeconds;
        this.RawProgres = (int)((100 * elapsedSinceStart) / this.EstimatedDuration.TotalSeconds);
      }
      else
      {
        this.RawProgres = 0;
      }

      this.Culprits.Clear();
      foreach (var culprit in ccStatus.Culprits)
      {
        var culpritModel = new CulpritViewModel();
        culpritModel.FullName = culprit.Name;
        this.Culprits.Add(culpritModel);
      }

      this.openBuildPageCommandData = new OpenWebPageCommandData();
      this.OpenBuildPageCommandData.Address = ccStatus.JobUrl;
      this.UpdateCalculatedFields();
    }
  }
}