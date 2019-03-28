namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.ServerBase;

  [SubjectType("Jenkins Project Status", Description = "Retrieve the current status of a Jenkins project.")]
  [ConfigurationItem(ProjectName, typeof(string), Optional = false, Priority = 300)]
  [ConfigurationItem(RedirectPlugin, typeof(bool), Priority = 400)] // defines use of Display URL API Plugin https://wiki.jenkins.io/display/JENKINS/Display+URL+API+Plugin
  public class JenkinsProject : ServerSubject
  {
    public const string ProjectName = "ProjectName";

    /// <summary>
    /// The redirect plugin tag.
    /// </summary>
    public const string RedirectPlugin = "RedirectPlugin";

    /// <summary>
    /// Initializes a new instance of the <see cref="JenkinsProject"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public JenkinsProject(SubjectConfiguration configuration)
      : base(configuration)
    {
    }

    public string Project => this.GetProject();

    private Status PreviousCheckStatus { get; set; }

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

    protected override async Task ExecuteQuery(CancellationToken cancellationToken, params string[] args)
    {
      var job = await JenkinsApi.GetJenkinsJob(this, cancellationToken);
      var latestBuild = await JenkinsApi.GetJenkinsBuild(this, job.LastBuild.Number, cancellationToken);
      this.CurrentStatus = CreateStatus(latestBuild);
      if (this.Snapshots.Count == 0 && this.MaxSnapshots > 0)
      {
        var startBuildNumber = latestBuild.Building ? latestBuild.Number - 1 : latestBuild.Number;
        var lastHistoryBuild = Math.Max(startBuildNumber - this.MaxSnapshots, 0);
        for (int i = lastHistoryBuild; i <= startBuildNumber; i++)
        {
          var buildStatus = await JenkinsApi.GetJenkinsBuild(this, i, cancellationToken);
          this.AddSnapshot(CreateStatus(buildStatus));
        }
      }
    }

    protected override bool ShouldTakeSnapshot(Status status)
    {
      if (this.PreviousCheckStatus != null)
      {
        if (!int.TryParse(this.PreviousCheckStatus.Properties[BuildPropertyKeys.Number], out int prevBuildNumber) || !int.TryParse(status.Properties[BuildPropertyKeys.Number], out int currentBuildNumber))
        {
          return false;
        }

        if ((status.State != ObservationState.Running && (status.State != this.PreviousCheckStatus.State || prevBuildNumber != currentBuildNumber)) || currentBuildNumber - prevBuildNumber > 1)
        {
          this.PreviousCheckStatus = status;
          return true;
        }
      }

      this.PreviousCheckStatus = status;
      return false;
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
