// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsConnector.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
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
  [ConfigurationItem(UserName, typeof(string), Priority = 410)]
  [ConfigurationItem(ApiToken, typeof(SecretProtector), Priority = 420)]
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

    private IList<JenkinsBuild>? recentBuilds;

    private JenkinsJob? batchJob;

    private bool usingBatch;

    private int failedTestsBuildNumber;

    private int failedTestsCount;

    private string? failedTests;

    private string? pipelineFailure;

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

    public string? AuthenticationUser => this.Configuration.ConfigurationItems.FirstOrDefault(item => item.Key == UserName)?.Value?.Trim();

    public string? EncryptedApiToken => this.Configuration.ConfigurationItems.FirstOrDefault(item => item.Key == ApiToken)?.Value;

    public bool HasApiCredentials => !string.IsNullOrWhiteSpace(this.AuthenticationUser) && !string.IsNullOrEmpty(this.EncryptedApiToken);

    private JenkinsStatus? PreviousCheckStatus { get; set; }

    public async Task<Status> SetVolunteer(int buildNumber, string? expectedOwner, bool claim, CancellationToken token)
    {
      var jobUrl = JenkinsApi.UrlHelper.ProjectUrl(this, false);
      if (claim)
      {
        if (!this.HasApiCredentials || this.CurrentStatus is not { Building: false, State: ObservationState.Failure or ObservationState.Unstable } current
            || current.BuildNumber != buildNumber || this.AuthenticationUser is not string owner)
        {
          throw new InvalidOperationException("Only the current failed or unstable build can be claimed. Configure Jenkins credentials first.");
        }

        var job = await this.api.GetJenkinsJob(this, token);
        if (job?.LastBuild is not { Building: false, Result: "FAILURE" or "UNSTABLE" } latest || latest.Number != buildNumber)
        {
          throw new InvalidOperationException("The current Jenkins build has changed. Refresh the project before volunteering.");
        }

        // Persist before the request so a crash or an uncertain network response cannot lose cleanup tracking.
        this.volunteers.Save(jobUrl, buildNumber, owner);
      }

      var build = await this.api.SetVolunteer(this, buildNumber, expectedOwner, claim, token);
      if (!claim)
      {
        this.volunteers.Remove(jobUrl, buildNumber);
      }

      var description = build.Description;
      if (this.CurrentStatus?.BuildNumber == buildNumber)
      {
        this.CurrentStatus.Details = description;
      }

      foreach (var snapshot in this.Snapshots.Where(snapshot => snapshot.Status.BuildNumber == buildNumber))
      {
        snapshot.Status.Details = description;
      }

      return this.CreateStatus(build);
    }

    public async Task<IReadOnlyCollection<Connector>> QueryStatusBatch(IReadOnlyCollection<Connector> connectors, CancellationToken token)
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

      var response = await this.api.GetPollingJobs(batch, token);
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
          await connector.QueryStatus(token);
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
    public override async Task<bool> CheckServerURL() => await this.IsReachableUrl(this.Address);

    /// <summary>
    /// Checks correctness of self project URL.
    /// </summary>
    /// <returns>true when fine, false when url is broken.</returns>
    public override async Task<bool> CheckProjectURL() => await this.IsReachableUrl(JenkinsApi.UrlHelper.ProjectUrl(this));

    protected override async Task ExecuteQuery(CancellationToken cancellationToken)
    {
      var previousBuildNumber = this.PreviousCheckStatus?.BuildNumber;
      await base.ExecuteQuery(cancellationToken);

      if (this.recentBuilds != null)
      {
        foreach (var build in this.recentBuilds.Where(build => !build.Building).OrderByDescending(build => build.Number).Take(MaxSnapshots))
        {
          foreach (var snapshot in this.Snapshots.Where(snapshot => snapshot.Status.BuildNumber == build.Number))
          {
            snapshot.Status.Details = build.Description;
            if (this.CurrentStatus?.BuildNumber == build.Number)
            {
              snapshot.Status.FailedTestCount = this.CurrentStatus.FailedTestCount;
              snapshot.Status.FailureDetails = this.CurrentStatus.FailureDetails;
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
        var builds = await this.api.GetBuilds(this, cancellationToken, 0, MaxSnapshots + 1);
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

    protected override async Task<Status?> GetCurrentStatus(CancellationToken cancellationToken)
    {
      var job = this.usingBatch ? this.batchJob : await this.api.GetJenkinsJob(this, cancellationToken);
      this.recentBuilds = job?.Builds ?? (this.usingBatch ? Array.Empty<JenkinsBuild>() : null);
      if (job == null)
      {
        if (this.usingBatch)
        {
          return new Status { ErrorMessage = Strings.ProjectNotAvailable, InvalidBuild = true };
        }

        if (!await this.CheckServerURL())
        {
          var status = new Status();
          status.ErrorMessage = Strings.ServerNotAvailable;
          status.InvalidBuild = true;
          return status;
        }

        if (!await this.CheckProjectURL())
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

      await this.ClearRecoveredVolunteers(job, cancellationToken);
      var failedCount = job.LastBuild.Actions?.OfType<JenkinsBuildAction>().Select(action => action.FailCount).DefaultIfEmpty().Max() ?? 0;
      if (this.failedTestsBuildNumber != job.LastBuild.Number || this.failedTestsCount != failedCount
          || this.CurrentStatus?.State != GetState(job.LastBuild))
      {
        this.failedTests = null;
        this.pipelineFailure = null;
        this.failedTestsBuildNumber = job.LastBuild.Number;
        this.failedTestsCount = failedCount;
      }

      if (GetState(job.LastBuild) is ObservationState.Failure or ObservationState.Unstable && failedCount > 0 && this.failedTests == null)
      {
        this.failedTests = await this.api.GetFailedTests(this, job.LastBuild.Number, cancellationToken);
      }

      if (GetState(job.LastBuild) is ObservationState.Failure or ObservationState.Unstable
          && string.IsNullOrWhiteSpace(this.failedTests) && this.pipelineFailure == null
          && !(job.LastBuild.Actions?.OfType<JenkinsBuildAction>()
            .Any(action => action.FoundFailureCauses?.OfType<JenkinsFailureCause>().Any(cause => !string.IsNullOrWhiteSpace(cause.Name)) == true) ?? false))
      {
        this.pipelineFailure = await this.api.GetPipelineFailure(this, job.LastBuild.Number, cancellationToken);
      }

      return this.CreateStatus(job.LastBuild);
    }

    protected override async Task<List<Status?>> GetHistory(CancellationToken cancellationToken)
    {
      var builds = this.recentBuilds ?? await this.api.GetBuilds(this, cancellationToken, 1, MaxSnapshots + 1);
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

    private async Task ClearRecoveredVolunteers(JenkinsJob job, CancellationToken token)
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
        foreach (var claim in this.volunteers.Read(jobUrl).Where(claim => claim.BuildNumber < successfulBuild).ToList())
        {
          try
          {
            token.ThrowIfCancellationRequested();
            var build = await this.api.GetJenkinsBuild(this, claim.BuildNumber, token);
            token.ThrowIfCancellationRequested();
            if (build == null || build.Number != claim.BuildNumber)
            {
              continue;
            }

            if (JenkinsVolunteer.GetValidatedOwner(build.Description) == claim.Owner)
            {
              build = await this.api.SetVolunteer(this, claim.BuildNumber, claim.Owner, false, token);
            }

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

    private Status CreateStatus(JenkinsBuild latestBuild)
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
          .OfType<JenkinsFailureCause>().Select(cause => cause.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().Take(5);
        var diagnostics = latestBuild.Number == this.failedTestsBuildNumber
          ? (string.IsNullOrWhiteSpace(this.failedTests) ? this.pipelineFailure : this.failedTests) : null;
        newStatus.FailureDetails = string.Join(Environment.NewLine, causes.Append(diagnostics).Where(value => !string.IsNullOrWhiteSpace(value)));
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
