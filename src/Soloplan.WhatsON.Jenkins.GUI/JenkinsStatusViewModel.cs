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
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Input;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  public class JenkinsStatusViewModel : BuildStatusViewModel
  {
    private readonly CustomCommand volunteerToFix = new CustomCommand();
    private readonly CustomCommand clearVolunteer = new CustomCommand();
    private ObservableCollection<UserViewModel>? committedToThisBuild;
    private bool changingVolunteer;
    private Status? buildStatus;
    private int failureRequests;

    public JenkinsStatusViewModel(ConnectorViewModel model)
      : base(model)
    {
      this.volunteerToFix.CanExecuteExternal += (_, e) => e.Cancel = !this.CanChangeVolunteer()
        || this.Volunteer != null || this.Building
        || this.BuildNumber != this.Parent.CurrentStatus.BuildNumber
        || (this.State != ObservationState.Failure && this.State != ObservationState.Unstable)
        || (this.Parent.CurrentStatus.State != ObservationState.Failure && this.Parent.CurrentStatus.State != ObservationState.Unstable);
      this.clearVolunteer.CanExecuteExternal += (_, e) => e.Cancel = !this.CanChangeVolunteer() || this.Volunteer == null;
      this.volunteerToFix.OnExecute += async (_, _) => await this.ChangeVolunteerAsync(true);
      this.clearVolunteer.OnExecute += async (_, _) => await this.ChangeVolunteerAsync(false);
    }

    public override ICommand VolunteerToFix => this.volunteerToFix;

    public override ICommand ClearVolunteer => this.clearVolunteer;

    public bool CulpritsAndLastCommittedDifferent { get; set; }

    public ObservableCollection<UserViewModel> CommittedToThisBuild =>
      this.committedToThisBuild ?? (this.committedToThisBuild = new ObservableCollection<UserViewModel>());

    public override void Update(Status? newStatus)
    {
      this.buildStatus = newStatus;
      this.SkippedTestCount = (newStatus as JenkinsStatus)?.SkippedTestCount ?? 0;
      base.Update(newStatus);
      this.VolunteerAssignments.Clear();
      var fixes = ReferenceEquals(this, this.Parent.CurrentStatus)
        ? (newStatus as JenkinsStatus)?.ActiveFixes ?? Array.Empty<JenkinsFix>() : Array.Empty<JenkinsFix>();
      this.Volunteer = newStatus is JenkinsStatus ? JenkinsVolunteer.GetOwner(newStatus.Details)
        ?? fixes.FirstOrDefault(fix => fix.BuildNumber == this.BuildNumber)?.Owner : null;
      this.VolunteerSummary = fixes.Count == 0 ? null
        : string.Join(", ", fixes.Select(fix => $"{fix.Owner} (#{fix.BuildNumber})"));
      foreach (var fix in fixes.Where(fix => fix.BuildNumber != this.BuildNumber))
      {
        var assignment = new JenkinsStatusViewModel(this.Parent);
        assignment.Update(new JenkinsStatus(ObservationState.Failure)
        {
          BuildNumber = fix.BuildNumber,
          Details = JenkinsVolunteer.SetOwner(null, fix.Owner),
        });
        this.VolunteerAssignments.Add(assignment);
      }

      this.RefreshVolunteerCommands();
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
        var culpritModel = new UserViewModel { FullName = culprit.FullName, Url = culprit.AbsoluteUrl };
        this.Culprits.Add(culpritModel);
      }

      foreach (var culprit in jenkinsStatus.CommittedToThisBuild ?? Enumerable.Empty<JenkinsUser>())
      {
        var culpritModel = new UserViewModel { FullName = culprit.FullName, Url = culprit.AbsoluteUrl };
        this.CommittedToThisBuild.Add(culpritModel);
      }

      this.CulpritsAndLastCommittedDifferent = this.Culprits.Count != this.CommittedToThisBuild.Count
                                               || this.Culprits.Any(culprit =>
                                                 this.CommittedToThisBuild.All(committer => committer.FullName != culprit.FullName));

      this.UpdateCalculatedFields();
    }

    public override Task LoadFailureDetailsAsync(CancellationToken token) => this.LoadFailureDetailsAsync(token, false);

    public override Task LoadExpandedFailureDetailsAsync(CancellationToken token) => this.LoadFailureDetailsAsync(token, true);

    private async Task LoadFailureDetailsAsync(CancellationToken token, bool expanded)
    {
      if (this.buildStatus is not Status status || this.Building || this.State is not (ObservationState.Failure or ObservationState.Unstable)
          || this.Parent.Connector is not JenkinsConnector connector)
      {
        return;
      }

      var number = this.BuildNumber;
      var project = JenkinsApi.UrlHelper.ProjectUrl(connector, false);
      var failed = status.FailedTestCount;
      var skipped = (status as JenkinsStatus)?.SkippedTestCount ?? 0;
      this.failureRequests++;
      this.IsLoadingFailureDetails = true;
      try
      {
        var details = await connector.LoadFailureDetailsAsync(status, token, expanded);
        token.ThrowIfCancellationRequested();
        if (this.BuildNumber == number && this.State == status.State && ReferenceEquals(this.Parent.Connector, connector)
            && JenkinsApi.UrlHelper.ProjectUrl(connector, false) == project && this.FailedTestCount == failed
            && ((this.buildStatus as JenkinsStatus)?.SkippedTestCount ?? 0) == skipped)
        {
          if (expanded)
          {
            this.ExpandedFailureDetails = details;
          }
          else
          {
            this.FailureDetails = details;
          }
        }
      }
      finally
      {
        this.IsLoadingFailureDetails = --this.failureRequests > 0;
      }
    }

    private bool CanChangeVolunteer() => !this.changingVolunteer && this.BuildNumber > 0
      && this.Parent.Connector is JenkinsConnector { HasApiCredentials: true };

    private void RefreshVolunteerCommands()
    {
      this.volunteerToFix.CanExecute(null);
      this.clearVolunteer.CanExecute(null);
    }

    private async Task ChangeVolunteerAsync(bool claim)
    {
      if (!(claim ? this.volunteerToFix : this.clearVolunteer).CanExecute(null)
          || this.Parent.Connector is not JenkinsConnector connector || this.BuildNumber is not int buildNumber)
      {
        return;
      }

      this.changingVolunteer = true;
      this.RefreshVolunteerCommands();
      try
      {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var status = await connector.SetVolunteerAsync(buildNumber, this.Volunteer, claim, timeout.Token);
        this.Update(status);
        this.Parent.Update(connector);
      }
      catch (Exception exception)
      {
        this.Parent.Update(connector);
        MessageBox.Show(Strings.VolunteerUpdateFailed + Environment.NewLine + exception.Message, Strings.VolunteerToFix, MessageBoxButton.OK, MessageBoxImage.Error);
      }
      finally
      {
        this.changingVolunteer = false;
        this.RefreshVolunteerCommands();
      }
    }
  }
}
