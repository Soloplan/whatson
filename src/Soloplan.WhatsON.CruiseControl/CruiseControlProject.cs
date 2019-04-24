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
  using Soloplan.WhatsON.ServerBase;

  [SubjectType("Cruise Control Project Status", Description = "Retrieve the current status of a Cruise Control project.")]
  [ConfigurationItem(ProjectName, typeof(string), Optional = false, Priority = 300)]
  public class CruiseControlProject : ServerSubject
  {
    private const string ProjectName = "ProjectName";

    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    private TimeSpan estimatedDuration = default(TimeSpan);

    public CruiseControlProject(SubjectConfiguration configuration)
      : base(configuration)
    {
    }

    public string Project => this.SubjectConfiguration.GetConfigurationByKey(CruiseControlProject.ProjectName).Value;

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

      if (this.PreviousCheckStatus != null && this.PreviousCheckStatus.State == ObservationState.Running && this.PreviousCheckStatus.BuildNumber < status.BuildNumber)
      {
        log.Debug("Changing previous build status.");
        if (projectData.LastBuildStatus == "Success")
        {
          log.Debug("Changing status to Success");
          this.PreviousCheckStatus.State = ObservationState.Success;
        }
        else if (projectData.LastBuildStatus == "Failure")
        {
          log.Debug("Changing status to Failure");
          this.PreviousCheckStatus.State = ObservationState.Failure;
        }
      }
    }

    protected override bool ShouldTakeSnapshot(Status status)
    {
      if (status is CruiseControlStatus currentStatus)
      {
        if (this.PreviousCheckStatus != null)
        {
          if (status.State != ObservationState.Running)
          {
            if (status.State != this.PreviousCheckStatus.State || this.PreviousCheckStatus.BuildNumber != currentStatus.BuildNumber)
            {
              log.Debug("Shoud take snapshot, build not running. {@status}, {@PreviousCheckStatus}", status, this.PreviousCheckStatus);
              this.estimatedDuration = this.PreviousCheckStatus.Duration;
              this.PreviousCheckStatus = currentStatus;
              return true;
            }
          }
          else
          {
            if (this.PreviousCheckStatus.BuildNumber != currentStatus.BuildNumber)
            {
              log.Debug("Shoud take snapshot, build running. {@status}, {@PreviousCheckStatus}", status, this.PreviousCheckStatus);
              this.AddSnapshot(this.PreviousCheckStatus);
              this.estimatedDuration = this.PreviousCheckStatus.Duration;
              this.PreviousCheckStatus = currentStatus;
              return false;
            }
          }
        }
        else
        {
          log.Debug("Initialize previous check status: {@status}", currentStatus);
          this.PreviousCheckStatus = currentStatus;
        }
      }

      return false;
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

      if (result.Building || result.CheckingModifications || result.Pending)
      {
        result.State = ObservationState.Running;
      }
      else if (job.LastBuildStatus == "Success")
      {
        result.State = ObservationState.Success;
      }
      else if (job.LastBuildStatus == "Failure")
      {
        result.State = ObservationState.Failure;
      }

      if (result.Building)
      {
        result.Duration = DateTime.Now - job.NextBuildTime;
      }

      result.EstimatedDuration = this.estimatedDuration;

      result.Time = DateTime.Now;
      return result;
    }

    private static void SetCulprits(CruiseControlJob job, CruiseControlStatus result)
    {
      if (job.Messages != null)
      {
        var breakers = job.Messages.Where(msg => msg.Kind == MessageKind.Breakers);
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