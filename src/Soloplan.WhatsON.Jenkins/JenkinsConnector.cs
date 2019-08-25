// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsConnector.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//    Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using NLog;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  [ConnectorType("Jenkins", Description = "Retrieve the current status of a Jenkins project.")]
  [ConfigurationItem(ProjectName, typeof(string), Optional = false, Priority = 300)]
  [ConfigurationItem(RedirectPlugin, typeof(bool), Priority = 400)] // defines use of Display URL API Plugin https://wiki.jenkins.io/display/JENKINS/Display+URL+API+Plugin
  [NotificationConfigurationItem(NotificationsVisbility, typeof(ConnectorNotificationConfiguration), Priority = 1600000000)]
  public class JenkinsConnector : Connector
  {
    public const string ProjectName = "ProjectName";

    /// <summary>
    /// The redirect plugin tag.
    /// </summary>
    public const string RedirectPlugin = "RedirectPlugin";

    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// API class for accessing Jenkins.
    /// </summary>
    private readonly IJenkinsApi api;

    /// <summary>
    /// Initializes a new instance of the <see cref="JenkinsConnector"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="api">API for Jenkins.</param>
    public JenkinsConnector(ConnectorConfiguration configuration, IJenkinsApi api)
      : base(configuration)
    {
      this.api = api;
    }

    public string Project => this.GetProject();

    private JenkinsStatus PreviousCheckStatus { get; set; }

    /// <summary>
    /// Gets the project.
    /// </summary>
    /// <returns>Project name.</returns>
    public string GetProject()
    {
      return this.ConnectorConfiguration.GetConfigurationByKey(JenkinsConnector.ProjectName).Value;
    }

    protected override async Task ExecuteQuery(CancellationToken cancellationToken, params string[] args)
    {
      var job = await this.api.GetJenkinsJob(this, cancellationToken);
      if (job?.LastBuild?.Number == null)
      {
        return;
      }

      var latestBuild = await this.api.GetJenkinsBuild(this, job.LastBuild.Number, cancellationToken);
      this.CurrentStatus = CreateStatus(latestBuild);
      if (this.Snapshots.Count == 0 && job.FirstBuild.Number < job.LastBuild.Number)
      {
        var startBuildNumber = latestBuild.Building ? latestBuild.Number - 1 : latestBuild.Number;
        var lastHistoryBuild = Math.Max(startBuildNumber - MaxSnapshots + 1, 0);
        lastHistoryBuild = Math.Max(lastHistoryBuild, job.FirstBuild.Number);
        log.Debug("Retrieving history from the server for builds {builds}", new { StartBuild = lastHistoryBuild, EndBuild = startBuildNumber - 1 });

        JenkinsStatus buildStatus = null;
        for (int i = lastHistoryBuild; i <= startBuildNumber; i++)
        {
          buildStatus = CreateStatus(await this.api.GetJenkinsBuild(this, i, cancellationToken));
          log.Debug("Retrieved status {buildStatus}", buildStatus);
          this.AddSnapshot(buildStatus);
        }

        this.PreviousCheckStatus = buildStatus;
      }

      if (this.PreviousCheckStatus != null && this.CurrentStatus is JenkinsStatus currentStatus)
      {
        if (currentStatus.BuildNumber - this.PreviousCheckStatus.BuildNumber > 1)
        {
          var start = this.PreviousCheckStatus.BuildNumber + 1;
          for (int i = start; i < currentStatus.BuildNumber; i++)
          {
            var buildStatus = CreateStatus(await this.api.GetJenkinsBuild(this, i, cancellationToken));
            if (this.ShouldTakeSnapshot(buildStatus))
            {
              this.AddSnapshot(buildStatus);
            }
          }
        }
      }
    }

    protected override bool ShouldTakeSnapshot(Status status)
    {
      log.Trace("Checking if snapshot should be taken...");
      if (status is JenkinsStatus currentStatus)
      {
        if (this.PreviousCheckStatus != null)
        {
          if (status.State != ObservationState.Running && (status.State != this.PreviousCheckStatus.State || this.PreviousCheckStatus.BuildNumber != currentStatus.BuildNumber))
          {
            log.Debug("Snapshot should be taken. {stat}", new { this.PreviousCheckStatus, CurrentStatus = currentStatus });
            this.PreviousCheckStatus = currentStatus;
            return true;
          }
        }
        else
        {
          this.PreviousCheckStatus = currentStatus;
        }
      }

      return false;
    }

    private static JenkinsStatus CreateStatus(JenkinsBuild latestBuild)
    {
      var newStatus = new JenkinsStatus(GetState(latestBuild))
      {
        Name = $"{latestBuild.DisplayName} ({TimeSpan.FromMilliseconds(latestBuild.Duration):g})",
        Time = DateTimeOffset.FromUnixTimeMilliseconds(latestBuild.Timestamp).UtcDateTime,
        Detail = latestBuild.Description,
      };

      newStatus.BuildNumber = latestBuild.Number;
      newStatus.DisplayName = latestBuild.DisplayName;
      newStatus.Building = latestBuild.Building;
      newStatus.DurationInMs = latestBuild.Duration;
      newStatus.EstimatedDurationInMs = latestBuild.EstimatedDuration;
      newStatus.Culprits = latestBuild.Culprits;

      newStatus.CommittedToThisBuild = latestBuild.ChangeSets?.SelectMany(p => p.ChangeSetItems)
        .Select(p => p.Author)
        .GroupBy(p => p.FullName)
        .Select(p => p.FirstOrDefault())
        .ToList();

      return newStatus;
    }

    private static ObservationState GetState(JenkinsBuild build)
    {
      if (string.IsNullOrWhiteSpace(build.Result))
      {
        return build.Building ? ObservationState.Running : ObservationState.Unknown;
      }

      if (Enum.TryParse<ObservationState>(build.Result, true, out var state))
      {
        return state;
      }

      return ObservationState.Unknown;
    }
  }
}
