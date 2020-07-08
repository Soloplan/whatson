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
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.CruiseControl.Model;
  using Soloplan.WhatsON.Model;

  [ConnectorType(ConnectorName, ConnectorDisplayName, Description = "Retrieve the current status of a Cruise Control project.")]
  [NotificationConfigurationItem(NotificationsVisbility, typeof(ConnectorNotificationConfiguration), SupportsUnstableNotify = false, Priority = 1600000000)]
  public class CruiseControlConnector : Connector
  {
    public const string ConnectorName = "CruiseControl";

    public const string ConnectorDisplayName = "Cruise Control.Net";

    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    private TimeSpan estimatedDuration = default;

    public CruiseControlConnector(ConnectorConfiguration configuration)
      : base(configuration)
    {
    }

    /// <summary>
    /// Checks correctness of self server URL.
    /// </summary>
    /// <returns>true when fine, false when url is broken.</returns>
    public override async Task<bool> CheckServerURL()
    {
      return await this.IsReachableUrl(this.Address);
    }

    /// <summary>
    /// Checks correctness of self project URL.
    /// </summary>
    /// <returns>true when fine, false when url is broken.</returns>
    public override async Task<bool> CheckProjectURL()
    {
      return await this.IsReachableUrl(this.directAddress);
    }

    protected override async Task<Status> GetCurrentStatus(CancellationToken cancellationToken)
    {
      var server = CruiseControlManager.GetServer(this.directAddress);
      var projectData = await server.GetProjectStatus(cancellationToken, this.Project, 5);
      if (projectData == null)
      {
        if (await this.CheckServerURL() == false)
        {
          var status = new Status();
          status.ErrorMessage = "Server not available";
          status.InvalidBuild = true;
          return status;
        }
        else if (await this.CheckProjectURL() == false)
        {
          var status = new Status();
          status.ErrorMessage = "Project not available";
          status.InvalidBuild = true;
          return status;
        }
      }

      if (projectData == null)
      {
        var status = new Status();
        status.ErrorMessage = "Project not available";
        status.InvalidBuild = true;
        return status;
      }

      if (projectData.LastBuildLabel == null)
      {
        var status = new Status();
        status.ErrorMessage = "No builds yet";
        status.InvalidBuild = true;
        return status;
      }

      log.Trace("Retrieved status for cruise control project {project}: {@projectData}", this.Project, projectData);
      return this.CreateStatus(projectData);
    }

    protected override async Task<List<Status>> GetHistory(CancellationToken cancellationToken)
    {
      var server = CruiseControlManager.GetServer(this.directAddress);
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

    private static Status CreateStatus(CruiseControlBuild build)
    {
      var result = new CruiseControlStatus();
      result.Building = false;
      result.BuildNumber = build.BuildNumber;
      result.Details = build.BuildLabel;
      result.Url = build.Url;
      result.Label = build.BuildLabel;
      result.Time = build.BuildTime;
      result.State = CcStatusToObservationStatus(build.Status);
      result.Name = build.Name;
      return result;
    }

    private CruiseControlStatus CreateStatus(CruiseControlJob job)
    {
      var result = new CruiseControlStatus();
      result.Name = job.Name;
      result.Details = job.Description;
      result.Building = job.Activity == ActivityConstants.Building;
      result.Pending = job.Activity == ActivityConstants.Pending;
      result.CheckingModifications = job.Activity == ActivityConstants.CheckingModifications;
      if (int.TryParse(job.LastBuildLabel, out var buildNr))
      {
        result.BuildNumber = buildNr;
      }

      result.Url = job.WebUrl;
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