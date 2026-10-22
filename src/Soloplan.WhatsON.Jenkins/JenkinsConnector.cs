// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsConnector.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using NLog;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  [ConnectorType(ConnectorName, ConnectorDisplayName, Description = nameof(Strings.JenkinsDescription))]
  [ConfigurationItem(
    RedirectPlugin,
    typeof(bool),
    Priority = 400)] // defines use of Display URL API Plugin https://wiki.jenkins.io/display/JENKINS/Display+URL+API+Plugin
  [NotificationConfigurationItem(NotificationsVisbility, typeof(ConnectorNotificationConfiguration), Priority = 1600000000)]
  [ConfigurationItem(UserName, typeof(string), Priority = 410, IsServerSetting = true)]
  [ConfigurationItem(ApiToken, typeof(SecretProtector), Priority = 420, IsServerSetting = true)]
  public class JenkinsConnector : Connector, IBatchedConnector
  {
    public const string ConnectorName = "Jenkins";

    public const string ConnectorDisplayName = "Jenkins";

    /// <summary>
    /// The redirect plugin tag.
    /// </summary>
    public const string RedirectPlugin = "RedirectPlugin";

    public const string UserName = "JenkinsUserName";

    public const string ApiToken = "JenkinsApiToken";

    private const long TicksInMillisecond = 10000;

    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// API class for accessing Jenkins.
    /// </summary>
    private readonly IJenkinsApi api;

    private readonly TimeProvider timeProvider;

    private readonly JenkinsVolunteerStore volunteers;

    private readonly JenkinsPollingPolicy polling = new JenkinsPollingPolicy();

    private readonly ConcurrentDictionary<(string Project, int Number, ObservationState State, int Failed, int Skipped, bool Expanded), string> failureDetails = new ConcurrentDictionary<(string, int, ObservationState, int, int, bool), string>();

    private readonly SemaphoreSlim failureDetailsLock = new SemaphoreSlim(1, 1);

    private IList<JenkinsBuild>? recentBuilds;

    private JenkinsJob? batchJob;

    private bool usingBatch;

    /// <summary>
    /// Initializes a new instance of the <see cref="JenkinsConnector" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="api">API for Jenkins.</param>
    public JenkinsConnector(ConnectorConfiguration configuration, IJenkinsApi api)
      : this(configuration, api, TimeProvider.System)
    {
    }

    internal JenkinsConnector(ConnectorConfiguration configuration, IJenkinsApi api, TimeProvider timeProvider)
      : this(configuration, api, timeProvider, JenkinsVolunteerStore.Instance)
    {
    }

    internal JenkinsConnector(ConnectorConfiguration configuration, IJenkinsApi api, TimeProvider timeProvider, JenkinsVolunteerStore volunteers)
      : base(configuration)
    {
      this.api = api;
      this.timeProvider = timeProvider;
      this.volunteers = volunteers;
    }

    public Uri? BatchAddress => Uri.TryCreate(this.Address?.TrimEnd('/') + "/", UriKind.Absolute, out var address) ? address : null;

    public string? AuthenticationUser => (this.Configuration.ServerConfiguration ?? this.Configuration).ConfigurationItems.FirstOrDefault(item => item.Key == UserName)?.Value?.Trim();

    public string? EncryptedApiToken => (this.Configuration.ServerConfiguration ?? this.Configuration).ConfigurationItems.FirstOrDefault(item => item.Key == ApiToken)?.Value;

    public bool HasApiCredentials => !string.IsNullOrWhiteSpace(this.AuthenticationUser) && !string.IsNullOrEmpty(this.EncryptedApiToken);

    private JenkinsStatus? PreviousCheckStatus { get; set; }

    public async Task<Status> SetVolunteerAsync(int buildNumber, string? expectedOwner, bool claim, CancellationToken token)
    {
      var jobUrl = JenkinsApi.UrlHelper.ProjectUrl(this, false);
      if (claim)
      {
        if (!this.HasApiCredentials || this.AuthenticationUser is not string owner)
        {
          throw new InvalidOperationException("Configure a Jenkins user name and API token before volunteering.");
        }

        var job = await this.api.GetJenkinsJobAsync(this, token);
        if (this.CurrentStatus is not { Building: false, State: ObservationState.Failure or ObservationState.Unstable } current
            || current.BuildNumber != buildNumber
            || job?.LastBuild is not { Building: false, Result: "FAILURE" or "UNSTABLE" } latest || latest.Number != buildNumber)
        {
          await this.QueryStatusAsync(token);
          var refreshed = this.CurrentStatus ?? throw new InvalidOperationException("Could not refresh the Jenkins build.");
          if (refreshed is not { Building: false, State: ObservationState.Failure or ObservationState.Unstable, BuildNumber: > 0 }
              || JenkinsVolunteer.GetOwner(refreshed.Details) != null)
          {
            return refreshed;
          }

          buildNumber = refreshed.BuildNumber;
          expectedOwner = null;
        }

        // Persist before the request so a crash or an uncertain network response cannot lose cleanup tracking.
        await this.volunteers.SaveAsync(jobUrl, buildNumber, owner, token).ConfigureAwait(false);
      }

      var build = await this.api.SetVolunteerAsync(this, buildNumber, expectedOwner, claim, token);
      var description = build.Description;
      if (this.CurrentStatus?.BuildNumber == buildNumber)
      {
        this.CurrentStatus.Details = description;
      }

      foreach (var snapshot in this.Snapshots.Where(snapshot => snapshot.Status.BuildNumber == buildNumber))
      {
        snapshot.Status.Details = description;
      }

      var panel = await this.api.SetJobVolunteerAsync(this, buildNumber, expectedOwner, claim ? this.AuthenticationUser : null, token);
      if (this.CurrentStatus is JenkinsStatus currentStatus)
      {
        currentStatus.ActiveFixes = JenkinsVolunteerPanel.GetFixes(panel);
      }

      if (!claim)
      {
        this.volunteers.Remove(jobUrl, buildNumber);
      }

      var status = this.CreateStatus(build);
      if (this.CurrentStatus?.BuildNumber == buildNumber)
      {
        status.ActiveFixes = JenkinsVolunteerPanel.GetFixes(panel);
      }

      return status;
    }

    public async Task<IReadOnlyCollection<Connector>> QueryStatusBatchAsync(IReadOnlyCollection<Connector> connectors, CancellationToken token)
    {
      var now = this.timeProvider.GetUtcNow();
      var due = connectors.Cast<JenkinsConnector>().Where(connector => connector.polling.NextDue <= now).OrderBy(connector => connector.polling.NextDue).ToList();
      var active = new Queue<JenkinsConnector>(due.Where(connector => connector.polling.Priority));
      var idle = new Queue<JenkinsConnector>(due.Where(connector => !connector.polling.Priority));
      var batch = new List<JenkinsConnector>();

      // Reserve every fourth place for the oldest idle job, even while active jobs keep running.
      while (active.Count > 0 || idle.Count > 0)
      {
        for (var index = 0; index < 3 && active.Count > 0; index++)
        {
          batch.Add(active.Dequeue());
        }

        if (idle.Count > 0)
        {
          batch.Add(idle.Dequeue());
        }
      }

      if (batch.Count == 0)
      {
        return Array.Empty<Connector>();
      }

      var response = await this.api.GetPollingJobsAsync(batch, token);
      var queried = new List<Connector>();
      foreach (var connector in batch)
      {
        token.ThrowIfCancellationRequested();
        if (!response.Jobs.TryGetValue(connector, out connector.batchJob))
        {
          if (response.Failed.Contains(connector))
          {
            connector.polling.Failed(this.timeProvider.GetUtcNow());
          }

          continue;
        }

        connector.usingBatch = true;
        try
        {
          await connector.QueryStatusAsync(token);
          connector.polling.Succeeded(connector.batchJob, this.timeProvider.GetUtcNow());
          queried.Add(connector);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
          throw;
        }
        catch (Exception exception)
        {
          connector.polling.Failed(this.timeProvider.GetUtcNow());
          log.Error(exception, "Could not apply Jenkins status for project {project}", connector.Project);
        }
        finally
        {
          connector.batchJob = null;
          connector.usingBatch = false;
        }
      }

      return queried;
    }

    /// <summary>
    /// Checks correctness of self server URL.
    /// </summary>
    /// <returns>true when fine, false when url is broken.</returns>
    public override async Task<bool> CheckServerUrlAsync() => await this.IsReachableUrlAsync(this.Address);

    /// <summary>
    /// Checks correctness of self project URL.
    /// </summary>
    /// <returns>true when fine, false when url is broken.</returns>
    public override async Task<bool> CheckProjectUrlAsync() => await this.IsReachableUrlAsync(JenkinsApi.UrlHelper.ProjectUrl(this));

    protected override async Task ExecuteQueryAsync(CancellationToken cancellationToken)
    {
      var previousBuildNumber = this.PreviousCheckStatus?.BuildNumber;
      await base.ExecuteQueryAsync(cancellationToken);

      if (this.recentBuilds != null)
      {
        foreach (var build in this.recentBuilds.Where(build => !build.Building).OrderByDescending(build => build.Number).Take(MaxSnapshots))
        {
          foreach (var snapshot in this.Snapshots.Where(snapshot => snapshot.Status.BuildNumber == build.Number))
          {
            snapshot.Status.Details = build.Description;
            var refreshed = this.CreateStatus(build);
            snapshot.Status.FailedTestCount = refreshed.FailedTestCount;
            snapshot.Status.FailureDetails = refreshed.FailureDetails;
            if (snapshot.Status is JenkinsStatus jenkinsStatus)
            {
              jenkinsStatus.SkippedTestCount = refreshed.SkippedTestCount;
            }
          }

          if (this.Snapshots.All(snapshot => snapshot.Status.BuildNumber != build.Number)
              && (this.Snapshots.Count < MaxSnapshots || build.Number > this.Snapshots.Min(snapshot => snapshot.Status.BuildNumber)))
          {
            this.AddSnapshot(this.CreateStatus(build));
          }
        }

        this.PreviousCheckStatus = this.Snapshots.FirstOrDefault()?.Status as JenkinsStatus;
        return;
      }

      if (previousBuildNumber.HasValue && this.CurrentStatus is JenkinsStatus currentStatus && this.Snapshots.Count != 0)
      {
        if (currentStatus.BuildNumber - previousBuildNumber.Value <= 1)
        {
          return;
        }

        log.Error(
          $"It was necessary to reevaluate history of jenkins job {this.Configuration.GetConfigurationByKey(Category)?.Value?.Trim()} / {this.Configuration.Name}, prev build number {previousBuildNumber.Value}, current build number {currentStatus.BuildNumber}");
        var builds = await this.api.GetBuildsAsync(this, cancellationToken, 0, MaxSnapshots + 1);
        foreach (var build in builds
          .Where(build => !build.Building && build.Number > previousBuildNumber.Value && build.Number < currentStatus.BuildNumber)
          .OrderByDescending(build => build.Number)
          .Take(MaxSnapshots - (currentStatus.State == ObservationState.Running ? 0 : 1)))
        {
          if (this.Snapshots.All(snapshot => snapshot.Status.BuildNumber != build.Number))
          {
            this.AddSnapshot(this.CreateStatus(build));
          }
        }

        this.PreviousCheckStatus = this.Snapshots.FirstOrDefault()?.Status as JenkinsStatus;
      }
    }

    public Task<string?> LoadFailureDetailsAsync(Status status, CancellationToken token) => this.LoadFailureDetailsAsync(status, token, false);

    public async Task<string?> LoadFailureDetailsAsync(Status status, CancellationToken token, bool expanded)
    {
      if (status.Building || status.BuildNumber <= 0 || status.State is not (ObservationState.Failure or ObservationState.Unstable))
      {
        return null;
      }

      var key = (Project: JenkinsApi.UrlHelper.ProjectUrl(this, false), Number: status.BuildNumber, State: status.State,
        Failed: status.FailedTestCount, Skipped: (status as JenkinsStatus)?.SkippedTestCount ?? 0, Expanded: expanded);
      var previousDetails = status.FailureDetails;
      await this.failureDetailsLock.WaitAsync(token).ConfigureAwait(false);
      try
      {
        if (this.failureDetails.TryGetValue(key, out var cached))
        {
          return cached;
        }

        var tests = key.Failed > 0 || key.Skipped > 0
          ? await this.api.GetFailedTestsAsync(this, key.Number, token, expanded).ConfigureAwait(false) : string.Empty;
        var console = key.Failed > 0 && !string.IsNullOrWhiteSpace(tests)
          ? string.Empty : await this.api.GetConsoleErrorsAsync(this, key.Number, token, expanded).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (JenkinsApi.UrlHelper.ProjectUrl(this, false) != key.Project)
        {
          return null;
        }

        var details = string.Join(Environment.NewLine, new[] { console, tests, previousDetails }.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (tests != null && console != null)
        {
          this.failureDetails[key] = details;
          while (this.failureDetails.Count > MaxSnapshots + 1)
          {
            this.failureDetails.TryRemove(this.failureDetails.Keys.OrderBy(item => item.Project == key.Project).ThenBy(item => item.Number).First(), out _);
          }
        }

        return details;
      }
      finally
      {
        this.failureDetailsLock.Release();
      }
    }

    protected override async Task<Status?> GetCurrentStatusAsync(CancellationToken cancellationToken)
    {
      var job = this.usingBatch ? this.batchJob : await this.api.GetJenkinsJobAsync(this, cancellationToken);
      this.recentBuilds = job?.Builds ?? (this.usingBatch ? Array.Empty<JenkinsBuild>() : null);
      if (job == null)
      {
        if (this.usingBatch)
        {
          return new Status { ErrorMessage = Strings.ProjectNotAvailable, InvalidBuild = true };
        }

        if (!await this.CheckServerUrlAsync())
        {
          var status = new Status();
          status.ErrorMessage = Strings.ServerNotAvailable;
          status.InvalidBuild = true;
          return status;
        }

        if (!await this.CheckProjectUrlAsync())
        {
          var status = new Status();
          status.ErrorMessage = Strings.ProjectNotAvailable;
          status.InvalidBuild = true;
          return status;
        }
      }

      if (job == null)
      {
        return new Status { ErrorMessage = Strings.ProjectNotAvailable, InvalidBuild = true };
      }

      this.IsDisabled = job.Disabled;

      if (job.LastBuild == null)
      {
        var status = new Status();
        status.ErrorMessage = Strings.NoBuildsYet;
        status.InvalidBuild = true;
        return status;
      }

      if (job.LastBuild.Number <= 0)
      {
        var status = this.CreateStatus(job.LastBuild);
        status.ErrorMessage = Strings.NoBuildNumber;
        return status;
      }

      await this.ClearRecoveredVolunteersAsync(job, cancellationToken);
      await this.SynchronizeVolunteerPanelAsync(job, cancellationToken);
      var currentStatus = this.CreateStatus(job.LastBuild);
      currentStatus.ActiveFixes = JenkinsVolunteerPanel.GetFixes(job.Description);
      return currentStatus;
    }

    protected override async Task<List<Status?>> GetHistoryAsync(CancellationToken cancellationToken)
    {
      var builds = this.recentBuilds ?? await this.api.GetBuildsAsync(this, cancellationToken, 1, MaxSnapshots + 1);
      var statuses = builds
        .Where(build => !build.Building && build.Number != this.CurrentStatus?.BuildNumber)
        .OrderByDescending(build => build.Number)
        .Take(MaxSnapshots)
        .Select(build => (Status?)this.CreateStatus(build)).ToList();
      this.PreviousCheckStatus = statuses.OfType<JenkinsStatus>().FirstOrDefault();
      return statuses;
    }

    protected override bool ShouldTakeSnapshot(Status? status)
    {
      var shouldTakeSnapshot = base.ShouldTakeSnapshot(status);
      if (shouldTakeSnapshot && status is JenkinsStatus jenkinsStatus)
      {
        this.PreviousCheckStatus = jenkinsStatus;
      }

      return shouldTakeSnapshot;
    }

    private static ObservationState GetState(JenkinsBuild build)
    {
      if (build.Building)
      {
        return ObservationState.Running;
      }

      if (string.IsNullOrWhiteSpace(build.Result))
      {
        return ObservationState.Unknown;
      }

      if (Enum.TryParse<ObservationState>(build.Result, true, out var state))
      {
        return state;
      }

      return ObservationState.Unknown;
    }

    private async Task ClearRecoveredVolunteersAsync(JenkinsJob job, CancellationToken token)
    {
      var successfulBuild = Math.Max(
        job.LastStableBuild?.Number ?? 0,
        job.LastBuild is { Building: false, Result: "SUCCESS" } latest ? latest.Number : 0);
      if (!this.HasApiCredentials || successfulBuild <= 0)
      {
        return;
      }

      try
      {
        var jobUrl = JenkinsApi.UrlHelper.ProjectUrl(this, false);
        foreach (var claim in (await this.volunteers.ReadAsync(jobUrl, token).ConfigureAwait(false)).Where(claim => claim.BuildNumber < successfulBuild).ToList())
        {
          try
          {
            token.ThrowIfCancellationRequested();
            var build = await this.api.GetJenkinsBuildAsync(this, claim.BuildNumber, token);
            token.ThrowIfCancellationRequested();
            if (build == null || build.Number != claim.BuildNumber)
            {
              continue;
            }

            if (JenkinsVolunteer.GetValidatedOwner(build.Description) == claim.Owner)
            {
              build = await this.api.SetVolunteerAsync(this, claim.BuildNumber, claim.Owner, false, token);
            }

            job.Description = await this.api.SetJobVolunteerAsync(this, claim.BuildNumber, claim.Owner, null, token);

            foreach (var recent in (job.Builds ?? Array.Empty<JenkinsBuild>()).Where(recent => recent.Number == claim.BuildNumber))
            {
              recent.Description = build.Description;
            }

            foreach (var snapshot in this.Snapshots.Where(snapshot => snapshot.Status.BuildNumber == claim.BuildNumber))
            {
              snapshot.Status.Details = build.Description;
            }

            this.volunteers.Remove(jobUrl, claim.BuildNumber);
          }
          catch (OperationCanceledException) when (token.IsCancellationRequested)
          {
            throw;
          }
          catch (Exception exception)
          {
            log.Warn(exception, "Could not clear Jenkins volunteer for build {build}; retrying on a later poll.", claim.BuildNumber);
          }
        }
      }
      catch (OperationCanceledException) when (token.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception exception)
      {
        log.Warn(exception, "Could not read Jenkins volunteer tracking for project {project}.", this.Project);
      }
    }

    private async Task SynchronizeVolunteerPanelAsync(JenkinsJob job, CancellationToken token)
    {
      if (!this.HasApiCredentials)
      {
        return;
      }

      try
      {
        var successfulBuild = Math.Max(job.LastStableBuild?.Number ?? 0,
          job.LastBuild is { Building: false, Result: "SUCCESS" } latest ? latest.Number : 0);
        foreach (var fix in JenkinsVolunteerPanel.GetFixes(job.Description).Where(fix => fix.BuildNumber < successfulBuild))
        {
          job.Description = await this.api.SetJobVolunteerAsync(this, fix.BuildNumber, fix.Owner, null, token);
        }

        // Resume a panel update after an interrupted request, including claims made by older whatsON versions.
        var jobUrl = JenkinsApi.UrlHelper.ProjectUrl(this, false);
        foreach (var claim in (await this.volunteers.ReadAsync(jobUrl, token).ConfigureAwait(false)).Where(claim => claim.BuildNumber > successfulBuild))
        {
          var fix = JenkinsVolunteerPanel.GetFixes(job.Description).FirstOrDefault(fix => fix.BuildNumber == claim.BuildNumber);
          var build = job.LastBuild?.Number == claim.BuildNumber ? job.LastBuild
            : (job.Builds ?? Array.Empty<JenkinsBuild>()).FirstOrDefault(build => build.Number == claim.BuildNumber);
          if (fix != null && (fix.Owner != claim.Owner || build == null))
          {
            continue;
          }

          build ??= await this.api.GetJenkinsBuildAsync(this, claim.BuildNumber, token);
          if (build == null)
          {
            continue;
          }

          var owner = JenkinsVolunteer.GetValidatedOwner(build.Description);
          if (owner == claim.Owner && fix == null)
          {
            job.Description = await this.api.SetJobVolunteerAsync(this, claim.BuildNumber, null, claim.Owner, token);
          }
          else if (owner != claim.Owner && fix != null)
          {
            job.Description = await this.api.SetJobVolunteerAsync(this, claim.BuildNumber, claim.Owner, null, token);
            this.volunteers.Remove(jobUrl, claim.BuildNumber);
          }
        }
      }
      catch (OperationCanceledException) when (token.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception exception)
      {
        log.Warn(exception, "Could not synchronize the Jenkins volunteer panel; retrying on a later poll.");
      }
    }

    private JenkinsStatus CreateStatus(JenkinsBuild latestBuild)
    {
      var newStatus = new JenkinsStatus(GetState(latestBuild))
      {
        Name = $"{latestBuild.DisplayName} ({TimeSpan.FromMilliseconds(latestBuild.Duration):g})",
        Time = DateTimeOffset.FromUnixTimeMilliseconds(latestBuild.Timestamp).UtcDateTime,
        Details = latestBuild.Description,
      };

      newStatus.BuildNumber = latestBuild.Number;
      newStatus.DisplayName = latestBuild.DisplayName;
      newStatus.Building = latestBuild.Building;
      newStatus.Duration = new TimeSpan(latestBuild.Duration * TicksInMillisecond);
      newStatus.EstimatedDuration = new TimeSpan(latestBuild.EstimatedDuration * TicksInMillisecond);
      newStatus.Culprits = latestBuild.Culprits?.OfType<JenkinsUser>().ToList() ?? new List<JenkinsUser>();
      newStatus.Url = JenkinsApi.UrlHelper.BuildUrl(this, newStatus.BuildNumber);
      newStatus.ErrorMessage = latestBuild.Result;
      if (newStatus.State is ObservationState.Failure or ObservationState.Unstable)
      {
        var actions = latestBuild.Actions?.OfType<JenkinsBuildAction>().ToList() ?? new List<JenkinsBuildAction>();
        newStatus.FailedTestCount = Math.Max(0, actions.Select(action => action.FailCount).DefaultIfEmpty().Max());
        var causes = actions.SelectMany(action => action.FoundFailureCauses ?? Array.Empty<JenkinsFailureCause>())
          .OfType<JenkinsFailureCause>().Select(cause => cause.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct();
        newStatus.SkippedTestCount = Math.Max(0, actions.Select(action => action.SkipCount).DefaultIfEmpty().Max());
        newStatus.FailureDetails = this.failureDetails.TryGetValue((JenkinsApi.UrlHelper.ProjectUrl(this, false), newStatus.BuildNumber, newStatus.State, newStatus.FailedTestCount, newStatus.SkippedTestCount, false), out var details)
          ? details : string.Join(Environment.NewLine, causes);
      }

      newStatus.CommittedToThisBuild = latestBuild.ChangeSets?.OfType<ChangeSet>().SelectMany(p => p.ChangeSetItems ?? Array.Empty<ChangeSetItem>())
        .OfType<ChangeSetItem>().Select(p => p.Author).OfType<JenkinsUser>().GroupBy(p => p.FullName)
        .Select(p => p.First()).ToList() ?? new List<JenkinsUser>();

      if (this.batchJob?.DetailsOmitted == true)
      {
        var previous = this.CurrentStatus is JenkinsStatus current && current.BuildNumber == latestBuild.Number
          ? current : this.Snapshots.Select(snapshot => snapshot.Status).OfType<JenkinsStatus>().FirstOrDefault(status => status.BuildNumber == latestBuild.Number);
        newStatus.DisplayName = previous?.DisplayName;
        newStatus.Name = previous?.Name ?? $"#{latestBuild.Number}";
        newStatus.Culprits = previous?.Culprits ?? new List<JenkinsUser>();
        newStatus.CommittedToThisBuild = previous?.CommittedToThisBuild ?? new List<JenkinsUser>();
      }

      return newStatus;
    }
  }
}
