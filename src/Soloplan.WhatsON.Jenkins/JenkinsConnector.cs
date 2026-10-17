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
  public class JenkinsConnector : Connector
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
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// API class for accessing Jenkins.
    /// </summary>
    private readonly IJenkinsApi api;

    private IList<JenkinsBuild> recentBuilds;

    /// <summary>
    /// Initializes a new instance of the <see cref="JenkinsConnector" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="api">API for Jenkins.</param>
    public JenkinsConnector(ConnectorConfiguration configuration, IJenkinsApi api)
      : base(configuration)
    {
      this.api = api;
    }

    private JenkinsStatus PreviousCheckStatus { get; set; }

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

      if (previousBuildNumber.HasValue && this.CurrentStatus is JenkinsStatus currentStatus && this.Snapshots.Count != 0)
      {
        if (currentStatus.BuildNumber - previousBuildNumber.Value <= 1)
        {
          return;
        }

        log.Error(
          $"It was necessary to reevaluate history of jenkins job {this.Configuration.GetConfigurationByKey(Category)?.Value?.Trim()} / {this.Configuration.Name}, prev build number {previousBuildNumber.Value}, current build number {currentStatus.BuildNumber}");
        var builds = this.recentBuilds ?? await this.api.GetBuilds(this, cancellationToken, 0, MaxSnapshots + 1);
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

    protected override async Task<Status> GetCurrentStatus(CancellationToken cancellationToken)
    {
      var job = await this.api.GetJenkinsJob(this, cancellationToken);
      this.recentBuilds = job?.Builds;
      if (job == null)
      {
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

      this.IsDisabled = job.Disabled;

      if (job.LastBuild == null)
      {
        var status = new Status();
        status.ErrorMessage = Strings.NoBuildsYet;
        status.InvalidBuild = true;
        return status;
      }

      if (job?.LastBuild?.Number == null)
      {
        var status = this.CreateStatus(job.LastBuild);
        status.ErrorMessage = Strings.NoBuildNumber;
        return status;
      }

      return this.CreateStatus(job.LastBuild);
    }

    protected override async Task<List<Status>> GetHistory(CancellationToken cancellationToken)
    {
      var builds = this.recentBuilds ?? await this.api.GetBuilds(this, cancellationToken, 1, MaxSnapshots + 1);
      var statuses = builds
        .Where(build => !build.Building && build.Number != this.CurrentStatus.BuildNumber)
        .OrderByDescending(build => build.Number)
        .Take(MaxSnapshots)
        .Select(this.CreateStatus).ToList();
      this.PreviousCheckStatus = statuses.OfType<JenkinsStatus>().FirstOrDefault();
      return statuses;
    }

    protected override bool ShouldTakeSnapshot(Status status)
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
      newStatus.Culprits = latestBuild.Culprits;
      newStatus.Url = JenkinsApi.UrlHelper.BuildUrl(this, newStatus.BuildNumber);
      newStatus.ErrorMessage = latestBuild.Result;

      newStatus.CommittedToThisBuild = latestBuild.ChangeSets?.SelectMany(p => p.ChangeSetItems).Select(p => p.Author).GroupBy(p => p.FullName)
        .Select(p => p.FirstOrDefault()).ToList();

      return newStatus;
    }
  }
}
