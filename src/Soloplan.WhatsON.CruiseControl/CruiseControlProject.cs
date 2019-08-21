// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlProject.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  using System;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using NLog;
  using Soloplan.WhatsON;
  using Soloplan.WhatsON.CruiseControl.Model;

  [ConnectorType("Cruise Control Project Status", Description = "Retrieve the current status of a Cruise Control project.")]
  [ConfigurationItem(ProjectName, typeof(string), Optional = false, Priority = 300)]
  [NotificationConfigurationItem(NotificationsVisbility, typeof(ConnectorNotificationConfiguration), SupportsUnstableNotify = false, Priority = 1600000000)]
  public class CruiseControlProject : ServerConnector
  {
    public const string ProjectName = "ProjectName";

    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    private TimeSpan estimatedDuration = default(TimeSpan);

    private TimeSpan cachedDuration = default(TimeSpan);

    public CruiseControlProject(ConnectorConfiguration configuration)
      : base(configuration)
    {
    }

    public string Project => this.ConnectorConfiguration.GetConfigurationByKey(CruiseControlProject.ProjectName).Value;

    private ICruiseControlServerManagerPlugIn ServerManager
    {
      get
      {
        var serverManager = PluginsManager.Instance.PlugIns.OfType<ICruiseControlServerManagerPlugIn>().ToList();
        var pluginCount = serverManager.Count;
        if (pluginCount != 1)
        {
          if (pluginCount < 1)
          {
            throw new InvalidOperationException($"No plugin of type {typeof(ICruiseControlServerManagerPlugIn)} found.");
          }

          throw new InvalidOperationException($"More then one plugins of type {typeof(ICruiseControlServerManagerPlugIn)} found.");
        }

        return serverManager.FirstOrDefault();
      }
    }

    private CruiseControlStatus PreviousCheckStatus { get; set; }

    protected override async Task ExecuteQuery(CancellationToken cancellationToken, params string[] args)
    {
      var server = this.ServerManager.GetServer(this.Address);
      var projectData = await server.GetProjectStatus(cancellationToken, this.Project, 5);
      log.Trace("Retrieved status for cruise control project {project}: {@projectData}", this.Project, projectData);
      var status = this.CreateStatus(projectData);
      log.Trace("Converted status for cruise control project {project}: {@status}", this.Project, status);
      this.CurrentStatus = status;

      if (status.Duration.TotalSeconds > 0)
      {
        this.cachedDuration = status.Duration;
      }

      if (this.PreviousCheckStatus != null)
      {
        if (status.State != ObservationState.Running)
        {
          if (status.State != this.PreviousCheckStatus.State || this.PreviousCheckStatus.BuildNumber != status.BuildNumber)
          {
            log.Debug("Shoud take snapshot, build not running. {@status}, {@PreviousCheckStatus}", status, this.PreviousCheckStatus);
            log.Debug("Changing estimated duration {proj}", new { ProjectName = this.Project, PrevEstimatedDurtion = this.estimatedDuration, NewEstimatedDuration = this.cachedDuration });
            this.estimatedDuration = this.cachedDuration;
            this.PreviousCheckStatus = status;
            this.PreviousCheckStatus.Duration = this.cachedDuration;
            this.AddOrUpdateSnapshot(this.PreviousCheckStatus);
          }
        }
        else
        {
          if (this.PreviousCheckStatus.BuildNumber < status.BuildNumber)
          {
            log.Debug("Shoud take snapshot, build running. {@status}, {@PreviousCheckStatus}", status, this.PreviousCheckStatus);
            this.PreviousCheckStatus.State = CcStatusToObservationStatus(projectData);
            log.Debug("Changing estimated duration {proj}", new { ProjectName = this.Project, PrevEstimatedDurtion = this.estimatedDuration, NewEstimatedDuration = this.cachedDuration });
            this.estimatedDuration = this.cachedDuration;
            this.PreviousCheckStatus.Duration = this.cachedDuration;
            this.AddOrUpdateSnapshot(this.PreviousCheckStatus);
          }
        }
      }
      else
      {
        log.Debug("Initialize previous check status: {@status}", status);
        this.PreviousCheckStatus = status;
        if (!this.PreviousCheckStatus.Building)
        {
          this.AddSnapshot(this.PreviousCheckStatus);
        }
      }
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

    /// <summary>
    /// Adds or updates snapshot based on <paramref name="status"/>. Update is done when build with the same number is already present.
    /// </summary>
    /// <param name="status">Status which should be added/updated.</param>
    private void AddOrUpdateSnapshot(CruiseControlStatus status)
    {
      var existingStatusIndex = this.Snapshots.IndexOf(this.Snapshots.FirstOrDefault(snap => (snap.Status as CruiseControlStatus)?.BuildNumber == status.BuildNumber));
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

          var culprit = new Culprit { Name = breaker };
          result.Culprits.Add(culprit);
        }
      }
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