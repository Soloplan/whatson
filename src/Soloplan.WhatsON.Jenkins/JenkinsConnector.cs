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
  public class JenkinsConnector : Connector, IBatchedConnector
  {
    public const string ConnectorName = "Jenkins";

    public const string ConnectorDisplayName = "Jenkins";

    /// <summary>
    /// The redirect plugin tag.
    /// </summary>
    public const string RedirectPlugin = "RedirectPlugin";

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

    private readonly JenkinsPollingPolicy polling = new JenkinsPollingPolicy();

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
      : base(configuration)
    {
      this.api = api;
      this.timeProvider = timeProvider;
    }

    public Uri? BatchAddress => Uri.TryCreate(this.Address?.TrimEnd('/') + "/", UriKind.Absolute, out var address) ? address : null;

    private JenkinsStatus? PreviousCheckStatus { get; set; }

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

      newStatus.CommittedToThisBuild = latestBuild.ChangeSets?.OfType<ChangeSet>().SelectMany(p => p.ChangeSetItems ?? Array.Empty<ChangeSetItem>())
        .OfType<ChangeSetItem>().Select(p => p.Author).OfType<JenkinsUser>().GroupBy(p => p.FullName)
        .Select(p => p.First()).ToList() ?? new List<JenkinsUser>();

      if (this.batchJob?.DetailsOmitted == true)
      {
        var previous = this.CurrentStatus is JenkinsStatus current && current.BuildNumber == latestBuild.Number
          ? current : this.Snapshots.Select(snapshot => snapshot.Status).OfType<JenkinsStatus>().FirstOrDefault(status => status.BuildNumber == latestBuild.Number);
        newStatus.Details = previous?.Details;
        newStatus.DisplayName = previous?.DisplayName;
        newStatus.Name = previous?.Name ?? $"#{latestBuild.Number}";
        newStatus.Culprits = previous?.Culprits ?? new List<JenkinsUser>();
        newStatus.CommittedToThisBuild = previous?.CommittedToThisBuild ?? new List<JenkinsUser>();
      }

      return newStatus;
    }
  }
}
