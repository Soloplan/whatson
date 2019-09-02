// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlConnector.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using NLog;
  using Soloplan.WhatsON;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.CruiseControl.Model;
  using Soloplan.WhatsON.Model;

  [ConnectorType(ConnectorName, Description = "Retrieve the current status of a Cruise Control project.")]
  [ConfigurationItem(ProjectName, typeof(string), Optional = false, Priority = 300)]
  [NotificationConfigurationItem(NotificationsVisbility, typeof(ConnectorNotificationConfiguration), SupportsUnstableNotify = false, Priority = 1600000000)]
  public class CruiseControlConnector : Connector
  {
    public const string ConnectorName = "CruiseControl";
    public const string ProjectName = "ProjectName";

    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    private TimeSpan estimatedDuration = default;

    private TimeSpan cachedDuration = default;

    public CruiseControlConnector(ConnectorConfiguration configuration)
      : base(configuration)
    {
    }

    public string Project => this.ConnectorConfiguration.GetConfigurationByKey(CruiseControlConnector.ProjectName).Value;

    protected async override Task<Status> GetCurrentStatus(CancellationToken cancellationToken)
    {
      var server = CruiseControlManager.GetServer(this.Address);
      var projectData = await server.GetProjectStatus(cancellationToken, this.Project, 5);
      log.Trace("Retrieved status for cruise control project {project}: {@projectData}", this.Project, projectData);
      return this.CreateStatus(projectData);
    }

    protected async override Task<List<Status>> GetHistory(CancellationToken cancellationToken)
    {
      var server = CruiseControlManager.GetServer(this.Address);
      var history = new List<Status>();

      var builds = await server.GetBuilds(this.Project);

      foreach (var build in builds)
      {
        history.Add(CreateStatus(build));
      }

      return history;
    }

    /// <summary>
    /// Creates statuses other then <see cref="ObservationState.Running"/> based on <paramref name="projectData"/>. Running build must be handled separately.
    /// </summary>
    /// <param name="projectData">Data retrieved from server.</param>
    /// <returns>Appropriate <see cref="ObservationState"/>.</returns>
    private static ObservationState CcStatusToObservationStatus(CruiseControlJob projectData)
    {
      if (projectData.LastBuildStatus == CcBuildStatus.Success && projectData.MessageList.MessagesSafe.All(msg => msg.Kind != MessageKind.FailingTasks && msg.Kind != MessageKind.Breakers && msg.Kind != MessageKind.BuildAbortedBy))
      {
        return ObservationState.Success;
      }

      if (projectData.LastBuildStatus == CcBuildStatus.Failure || projectData.LastBuildStatus == CcBuildStatus.Exception || projectData.MessageList.MessagesSafe.Any(msg => msg.Kind != MessageKind.FailingTasks && msg.Kind != MessageKind.Breakers))
      {
        return ObservationState.Failure;
      }

      return ObservationState.Unknown;
    }

    private static ObservationState CcStatusToObservationStatus(CcBuildStatus status)
    {
      switch (status)
      {
        case CcBuildStatus.Success:
          return ObservationState.Success;
        case CcBuildStatus.Exception:
        case CcBuildStatus.Failure:
          return ObservationState.Failure;
        case CcBuildStatus.Cancelled:
        case CcBuildStatus.Unknown:
        default:
          return ObservationState.Unknown;
      }
    }

    private static void SetCulprits(CruiseControlJob job, CruiseControlStatus result)
    {
      var breakers = job.MessageList.MessagesSafe.Where(msg => msg.Kind == MessageKind.Breakers);
      foreach (var breakersLine in breakers)
      {
        foreach (var breaker in breakersLine.Text.Split(',').Select(brk => brk.Trim()).Where(brk => !string.IsNullOrEmpty(brk)))
        {
          if (result.Culprits.Any(brk => brk.Name == breaker))
          {
            continue;
          }

          var culprit = new CruiseControlUser { Name = breaker };
          result.Culprits.Add(culprit);
        }
      }
    }

    /// <summary>
    /// Adds or updates snapshot based on <paramref name="status"/>. Update is done when build with the same number is already present.
    /// </summary>
    /// <param name="status">Status which should be added/updated.</param>
    private void AddOrUpdateSnapshot(Status status)
    {
      var existingStatusIndex = this.Snapshots.IndexOf(this.Snapshots.FirstOrDefault(snap => snap.Status.BuildNumber == status.BuildNumber));
      if (existingStatusIndex >= 0)
      {
        log.Debug("Changes exist for build cruise control project {proj}", new { ProjectName = this.Project, Build = status.BuildNumber });
        this.Snapshots.RemoveAt(existingStatusIndex);
        this.Snapshots.Insert(existingStatusIndex, new Snapshot(status));
      }
      else
      {
        this.AddSnapshot(status);
      }
    }

    private static Status CreateStatus(CruiseControlBuild build)
    {
      var result = new CruiseControlStatus();
      result.Building = false;
      result.BuildNumber = build.BuildNumber;
      result.Duration = build.Duration;
      result.State = CcStatusToObservationStatus(build.Status);
      result.Name = build.Id;
      return result;
    }

    private CruiseControlStatus CreateStatus(CruiseControlJob job)
    {
      var result = new CruiseControlStatus();
      result.Name = job.Name;
      result.Detail = job.Description;
      result.Building = job.Activity == ActivityConstants.Building;
      result.Pending = job.Activity == ActivityConstants.Pending;
      result.CheckingModifications = job.Activity == ActivityConstants.CheckingModifications;
      if (int.TryParse(job.LastBuildLabel, out var buildNr))
      {
        result.BuildNumber = buildNr;
      }

      result.JobUrl = job.WebUrl;
      SetCulprits(job, result);

      result.NextBuildTime = job.NextBuildTime;
      result.LastBuildTime = job.LastBuildTime;

      if (result.Building)
      {
        result.State = ObservationState.Running;
      }
      else
      {
        result.State = CcStatusToObservationStatus(job);
      }

      if (result.Building)
      {
        result.Duration = DateTime.Now - job.NextBuildTime;
        result.Time = job.NextBuildTime.ToUniversalTime();
      }
      else
      {
        result.Time = DateTime.UtcNow;
      }

      result.EstimatedDuration = this.estimatedDuration;

      return result;
    }

    private class ActivityConstants
    {
      public const string Building = "Building";
      public const string Sleeping = "Sleeping";
      public const string CheckingModifications = "CheckingModifications";
      public const string Pending = "Pending";
    }
  }
}