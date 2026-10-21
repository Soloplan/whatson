// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuildStatusViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System;
  using System.Collections.ObjectModel;
  using System.Windows.Input;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  public class BuildStatusViewModel : StatusViewModel
  {
    private ObservableCollection<UserViewModel>? culprits;

    public BuildStatusViewModel(ConnectorViewModel model)
      : base(model)
    {
    }

    /// <summary>
    /// Gets the command for opening the build webPage.
    /// </summary>
    public virtual OpenWebPageCommand OpenBuildPage { get; } = new OpenWebPageCommand();

    public virtual CopyBuildLabelCommand CopyBuildLabel { get; } = new CopyBuildLabelCommand();

    public virtual ICommand? VolunteerToFix => null;

    public virtual ICommand? ClearVolunteer => null;

    public string? Volunteer { get; protected set; }

    public string? VolunteerDisplayName => this.Volunteer?.ToUpperInvariant();

    public ObservableCollection<UserViewModel> Culprits => this.culprits ?? (this.culprits = new ObservableCollection<UserViewModel>());

    public int? BuildNumber { get; protected set; }

    public string? DisplayName { get; protected set; }

    public override string? Label
    {
      get
      {
        // if a custom label is set, use it
        if (!string.IsNullOrWhiteSpace(base.Label))
        {
          return base.Label;
        }

        if (!string.IsNullOrEmpty(this.DisplayName))
        {
          // #123 some displayname
          if (this.BuildNumber.HasValue && this.DisplayName.Contains($"#{this.BuildNumber.Value}"))
          {
            return this.DisplayName;
          }

          // another displayName without number (#123)
          return $"{this.DisplayName} (#{this.BuildNumber})";
        }

        if (this.BuildNumber.HasValue)
        {
          // #123
          return $"#{this.BuildNumber.Value}";
        }

        return string.Empty;
      }

      protected set => base.Label = value;
    }

    public string? Url { get; set; }

    public bool Building { get; protected set; }

    public DateTime FinishTime => this.Time + this.Duration;

    public override DateTime Time
    {
      get => base.Time;
      protected set => base.Time = value;
    }

    public TimeSpan Duration { get; protected set; }

    public TimeSpan EstimatedDuration { get; protected set; }

    public TimeSpan EstimatedRemaining { get; set; }

    public TimeSpan BuildTimeExcedingEstimation { get; set; }

    /// <summary>
    /// Gets or sets progress shown in progress bar and displayed in GUI.
    /// The value is processed:
    /// <list type="">
    ///   <item>0 is changed to 1 to prevent progress bar showing unknown value.</item>
    ///   <item>Changed to 0 if build is taking longer then expected to show unknown value.</item>
    ///   <item>Otherwise the value from build server is shown.</item>
    /// </list>
    /// </summary>
    public int Progress { get; protected set; }

    /// <summary>
    /// Gets or sets the true value of progress. Can get above 100% if the build takes longer then expected.
    /// </summary>
    public int RawProgress { get; protected set; }

    public bool BuildingNoLongerThenExpected { get; set; }

    public bool BuildingLongerThenExpected { get; set; }

    public override void Update(Status? newStatus)
    {
      base.Update(newStatus);
      if (newStatus == null)
      {
        return;
      }

      this.BuildNumber = newStatus.BuildNumber;
      this.Building = newStatus.Building;
      this.Duration = newStatus.Duration;
      this.EstimatedDuration = newStatus.EstimatedDuration;
      this.Label = newStatus.Label;
      this.Url = newStatus.Url;
      this.ErrorMessage = newStatus.ErrorMessage;

      this.UpdateCalculatedFields();
    }

    protected void UpdateCalculatedFields()
    {
      this.UpdateStateFlags();
      this.UpdateEstimatedRemaining();
      this.UpdateProgress();
    }

    /// <summary>
    /// Updates flags used to control visibility of controls based on <see cref="State" />.
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
    /// Updates <see cref="Progress" /> based on other parameters.
    /// </summary>
    /// <remarks>
    /// Must be called when <see cref="BuildingNoLongerThenExpected" />, <see cref="BuildingLongerThenExpected" /> and
    /// <see cref="RawProgress" /> are calculated
    /// and won't change. It is important not to change values when <see cref="BuildingLongerThenExpected" /> because it
    /// resets the indeterminate progress bar
    /// and the animation looks bad.
    /// </remarks>
    private void UpdateProgress()
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
