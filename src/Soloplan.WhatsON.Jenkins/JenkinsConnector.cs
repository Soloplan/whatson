// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsConnector.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//    Licensed under the MIT License. See License-file in the project root for license information.
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
  using Soloplan.WhatsON.Model;

  [ConnectorType(ConnectorName, Description = "Retrieve the current status of a Jenkins project.")]
  [ConfigurationItem(ProjectName, typeof(string), Optional = false, Priority = 300)]
  [ConfigurationItem(RedirectPlugin, typeof(bool), Priority = 400)] // defines use of Display URL API Plugin https://wiki.jenkins.io/display/JENKINS/Display+URL+API+Plugin
  [NotificationConfigurationItem(NotificationsVisbility, typeof(ConnectorNotificationConfiguration), Priority = 1600000000)]
  public class JenkinsConnector : Connector
  {
    private const long TicksInMillisecond = 10000;

    public const string ConnectorName = "Jenkins";

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

    /// <summary>
    /// Gets the project.
    /// </summary>
    /// <returns>Project name.</returns>
    public string GetProject()
    {
      return this.ConnectorConfiguration.GetConfigurationByKey(JenkinsConnector.ProjectName).Value;
    }

    protected async override Task<Status> GetCurrentStatus(CancellationToken cancellationToken)
    {
      var job = await this.api.GetJenkinsJob(this, cancellationToken);
      if (job?.LastBuild?.Number == null)
      {
        return null;
      }

      var latestBuild = await this.api.GetJenkinsBuild(this, job.LastBuild.Number, cancellationToken);
      return CreateStatus(latestBuild);
    }

    protected async override Task<List<Status>> GetHistory(CancellationToken cancellationToken)
    {
      var builds = await this.api.GetBuilds(this, cancellationToken, 1, MaxSnapshots + 1);
      return builds.Select(build => CreateStatus(build)).ToList();
    }

    private static Status CreateStatus(JenkinsBuild latestBuild)
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
      newStatus.Duration = new TimeSpan(latestBuild.Duration * TicksInMillisecond);
      newStatus.EstimatedDuration = new TimeSpan(latestBuild.EstimatedDuration * TicksInMillisecond);
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
