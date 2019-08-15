﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuildStatusViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System;
  using System.Collections.ObjectModel;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Jenkins.GUI;

  public abstract class BuildStatusViewModel : StatusViewModel
  {
    private TimeSpan estimatedDuration;
    private TimeSpan duration;
    private bool building;
    private int? buildNumber;

    private int progress;

    private int rawProgress;

    private bool buildingNoLongerThenExpected;

    private bool buildingLongerThenExpected;

    private TimeSpan estimatedRemaining;

    private TimeSpan buildTimeExcedingEstimation;

    private ObservableCollection<CulpritViewModel> culprits;

    public BuildStatusViewModel(ConnectorViewModel model)
      : base(model)
    {
    }

    /// <summary>
    /// Command for opening builds webPage.
    /// </summary>
    public virtual OpenWebPageCommand OpenBuildPage { get; } = new OpenWebPageCommand();

    public abstract OpenWebPageCommandData OpenBuildPageCommandData { get; }

    public ObservableCollection<CulpritViewModel> Culprits => this.culprits ?? (this.culprits = new ObservableCollection<CulpritViewModel>());

    public int? BuildNumber
    {
      get
      {
        return this.buildNumber;
      }

      protected set
      {

        if (this.buildNumber != value)
        {
          this.buildNumber = value;
          this.OnPropertyChanged();
        }
      }
    }

    public bool Building
    {
      get
      {
        return this.building;
      }

      protected set
      {
        if (this.building != value)
        {
          this.building = value;
          this.OnPropertyChanged();
        }
      }
    }

    public TimeSpan Duration
    {
      get
      {
        return this.duration;
      }

      protected set
      {
        if (this.duration != value)
        {
          this.duration = value;
          this.OnPropertyChanged();
        }
      }
    }

    public TimeSpan EstimatedDuration
    {
      get
      {
        return this.estimatedDuration;
      }

      protected set
      {
        if (this.estimatedDuration != value)
        {
          this.estimatedDuration = value;
          this.OnPropertyChanged();
        }
      }
    }

    public TimeSpan EstimatedRemaining
    {
      get => this.estimatedRemaining;
      set
      {
        if (this.estimatedRemaining != value)
        {
          this.estimatedRemaining = value;
          this.OnPropertyChanged();
        }
      }
    }

    public TimeSpan BuildTimeExcedingEstimation
    {
      get => this.buildTimeExcedingEstimation;
      set
      {
        if (this.buildTimeExcedingEstimation != value)
        {
          this.buildTimeExcedingEstimation = value;
          this.OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Gets or sets progress shown in progress bar and displayed in GUI.
    /// The value is processed:
    /// <list type="">
    /// <item>0 is changed to 1 to prevent progress bar showing unknown value.</item>
    /// <item>Changed to 0 if build is taking longer then expected to show unknown value.</item>
    /// <item>Otherwise the value from build server is shown.</item>
    /// </list>
    /// </summary>
    public int Progress
    {
      get
      {
        return this.progress;
      }

      protected set
      {
        if (this.progress != value)
        {
          this.progress = value;
          this.OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Gets or sets the true value of progress. Can get above 100% if the build takes longer then expected.
    /// </summary>
    public int RawProgress
    {
      get => this.rawProgress;
      protected set
      {
        this.rawProgress = value;
        this.OnPropertyChanged();
      }
    }

    public bool BuildingNoLongerThenExpected
    {
      get => this.buildingNoLongerThenExpected;
      set
      {
        if (this.buildingNoLongerThenExpected != value)
        {
          this.buildingNoLongerThenExpected = value;
          this.OnPropertyChanged();
        }
      }
    }

    public bool BuildingLongerThenExpected
    {
      get => this.buildingLongerThenExpected;
      set
      {
        if (this.buildingLongerThenExpected != value)
        {
          this.buildingLongerThenExpected = value;
          this.OnPropertyChanged();
        }
      }
    }

    protected void UpdateCalculatedFields()
    {
      this.UpdateStateFlags();
      this.UpdateEstimatedRemaining();
      this.UpdateProgres();
    }

    /// <summary>
    /// Updates flags used to control visibility of controls based on <see cref="State"/>.
    /// </summary>
    protected override void UpdateStateFlags()
    {
      this.Succees = false;
      this.Failure = false;
      this.Unknown = false;
      this.Unstable = false;
      if (!this.Building)
      {
        base.UpdateStateFlags();
      }
    }

    /// <summary>
    /// Updates flags controlling visibility of progress bar and the progress bar buttons.
    /// </summary>
    private void UpdateEstimatedRemaining()
    {
      if (!this.Building)
      {
        this.BuildingNoLongerThenExpected = false;
        this.BuildingLongerThenExpected = false;
        this.EstimatedDuration = TimeSpan.Zero;
      }
      else
      {
        this.BuildingNoLongerThenExpected = this.EstimatedDuration > this.Duration;
        this.BuildingLongerThenExpected = !this.BuildingNoLongerThenExpected;
        if (this.BuildingNoLongerThenExpected)
        {
          this.EstimatedRemaining = this.EstimatedDuration - this.Duration;
          this.BuildTimeExcedingEstimation = TimeSpan.Zero;
        }
        else
        {
          this.EstimatedRemaining = TimeSpan.Zero;
          this.BuildTimeExcedingEstimation = this.Duration - this.EstimatedDuration;
        }
      }
    }

    /// <summary>
    /// Updates <see cref="Progress"/> based on other parameters.
    /// </summary>
    /// <remarks>
    /// Must be called when <see cref="BuildingNoLongerThenExpected"/>, <see cref="BuildingLongerThenExpected"/> and <see cref="RawProgress"/> are calculated
    /// and won't change. It is important not to change values when <see cref="BuildingLongerThenExpected"/> because it resets the indeterminate progress bar
    /// and the animation looks bad.
    /// </remarks>
    private void UpdateProgres()
    {
      if (this.BuildingNoLongerThenExpected && this.RawProgress == 0)
      {
        this.Progress = 1;
      }
      else if (this.BuildingLongerThenExpected)
      {
        this.Progress = 0;
      }
      else
      {
        this.Progress = this.RawProgress;
      }
    }
  }
}