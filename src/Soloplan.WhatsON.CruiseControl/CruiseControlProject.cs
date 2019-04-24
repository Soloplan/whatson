// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlProject.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl
{
  using System;
  using System.Globalization;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using Soloplan.WhatsON;
  using Soloplan.WhatsON.CruiseControl.Model;
  using Soloplan.WhatsON.ServerBase;

  [SubjectType("Cruise Control Project Status", Description = "Retrieve the current status of a Cruise Control project.")]
  [ConfigurationItem(ProjectName, typeof(string), Optional = false, Priority = 300)]
  public class CruiseControlProject : ServerSubject
  {
    private const string ProjectName = "ProjectName";

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


    /// <summary>
    /// Gets the project.
    /// </summary>
    /// <returns>Project name.</returns>
    public string GetProject()
    {
      return this.SubjectConfiguration.GetConfigurationByKey(CruiseControlProject.ProjectName).Value;
    }

    protected override async Task ExecuteQuery(CancellationToken cancellationToken, params string[] args)
    {
      var server = this.ServerManager.GetServer(this.Address);
      var projectData = await server.GetProjectStatus(cancellationToken, this.Project, 5);
      var status = CreateStatus(projectData);
      this.CurrentStatus = status;

      if (this.PreviousCheckStatus != null && this.PreviousCheckStatus.BuildNumber < status.BuildNumber)
      {
        if (projectData.LastBuildStatus == "Success")
        {
          this.PreviousCheckStatus.State = ObservationState.Success;
        }
        else if (projectData.LastBuildStatus == "Failure")
        {
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
              this.estimatedDuration = this.PreviousCheckStatus.Duration;
              this.PreviousCheckStatus = currentStatus;
              return true;
            }
          }
          else
          {
            if (this.PreviousCheckStatus.BuildNumber != currentStatus.BuildNumber)
            {
              this.AddSnapshot(this.PreviousCheckStatus);
              this.estimatedDuration = this.PreviousCheckStatus.Duration;
              this.PreviousCheckStatus = currentStatus;
              return false;
            }
          }
        }
        else
        {
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