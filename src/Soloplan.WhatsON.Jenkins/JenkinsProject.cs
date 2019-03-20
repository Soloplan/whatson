namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.ServerBase;

  [SubjectType("Jenkins Project Status", Description = "Retrieve the current status of a Jenkins project.")]
  [ConfigurationItem(ProjectName, typeof(string))]
  public class JenkinsProject : ServerSubject
  {
    public const string ProjectName = "ProjectName";

    /// <summary>
    /// Initializes a new instance of the <see cref="JenkinsProject"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public JenkinsProject(SubjectConfiguration configuration)
      : base(configuration)
    {
    }

    protected string Project => this.GetProject();

    /// <summary>
    /// Gets the port number.
    /// </summary>
    public override int Port
    {
      get
      {
        var configItem = this.SubjectConfiguration.GetConfigurationByKey(ServerPort);
        if (configItem != null)
        {
          return int.TryParse(configItem.Value, out var port) ? port : GetDefaultPort(this.Address);
        }

        return 80;
      }
    }

    /// <summary>
    /// Gets the project.
    /// </summary>
    /// <returns>Project name.</returns>
    public string GetProject()
    {
      return this.SubjectConfiguration.GetConfigurationByKey(JenkinsProject.ProjectName).Value;
    }

    protected override void ExecuteQuery(params string[] args)
    {
      var job = JenkinsApi.GetJenkinsJob(this);
      var latestBuild = JenkinsApi.GetJenkinsBuild(this, job.LastBuild.Number);
      this.CurrentStatus = CreateStatus(latestBuild);
    }

    private static Status CreateStatus(JenkinsBuild latestBuild)
    {
      var newStatus = new Status(GetState(latestBuild))
      {
        Name = $"{latestBuild.DisplayName} ({TimeSpan.FromMilliseconds(latestBuild.Duration):g})",
        Time = DateTimeOffset.FromUnixTimeMilliseconds(latestBuild.Timestamp).UtcDateTime,
        Detail = latestBuild.Description,
      };

      newStatus.Properties[BuildPropertyKeys.Number] = $"{latestBuild.Number}";
      newStatus.Properties[BuildPropertyKeys.Building] = $"{latestBuild.Building}";
      newStatus.Properties[BuildPropertyKeys.Duration] = $"{latestBuild.Duration}";
      newStatus.Properties[BuildPropertyKeys.EstimatedDuration] = $"{latestBuild.EstimatedDuration}";

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

    private static int GetDefaultPort(string address)
    {
      if (!string.IsNullOrWhiteSpace(address) && address.StartsWith("https", StringComparison.InvariantCultureIgnoreCase))
      {
        return 443;
      }

      return 80;
    }

    public static class BuildPropertyKeys
    {
      public const string Number = "BuildNumber";
      public const string Building = "Building";
      public const string Duration = "Duration";
      public const string EstimatedDuration = "EstimatedDuration";
    }
  }
}
