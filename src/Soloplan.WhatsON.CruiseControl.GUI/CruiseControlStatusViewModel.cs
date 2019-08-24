// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlStatusViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.GUI
{
  using System;
  using Soloplan.WhatsON.CruiseControl.Model;
  using Soloplan.WhatsON.GUI.Common;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  public class CruiseControlStatusViewModel : BuildStatusViewModel
  {
    private OpenWebPageCommandData openBuildPageCommandData;

    /// <summary>
    /// The backing field for <see cref="BuildTimeUnknown"/>.
    /// </summary>
    private bool buildTimeUnknown;

    public CruiseControlStatusViewModel(ConnectorViewModel model)
      : base(model)
    {
    }

    public override OpenWebPageCommandData OpenBuildPageCommandData => this.openBuildPageCommandData;

    /// <summary>
    /// Gets or sets a value indicating whether estimated build time is known.
    /// </summary>
    public bool BuildTimeUnknown
    {
      get => this.buildTimeUnknown;
      set
      {
        if (this.buildTimeUnknown != value)
        {
          this.buildTimeUnknown = value;
          this.OnPropertyChanged();
        }
      }
    }

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
        var elapsedSinceStart = (DateTime.Now - ccStatus.NextBuildTime).TotalSeconds;
        this.RawProgress = (int)((100 * elapsedSinceStart) / this.EstimatedDuration.TotalSeconds);
      }
      else
      {
        this.RawProgress = 0;
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

      if (this.State == ObservationState.Running && this.EstimatedDuration.TotalSeconds < 1)
      {
        this.BuildingLongerThenExpected = false;
        this.BuildingNoLongerThenExpected = false;
        this.BuildTimeUnknown = true;
      }
      else
      {
        this.BuildTimeUnknown = false;
      }
    }
  }
}