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
    /// API class for accessing Jenkins.
    /// </summary>
    private IJenkinsApi api;

    /// <summary>
    /// Initializes a new instance of the <see cref="JenkinsProject"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="api">API for Jenkins.</param>
    public JenkinsProject(SubjectConfiguration configuration, IJenkinsApi api)
      : base(configuration)
    {
      this.api = api;
    }

    public string Project => this.GetProject();

    private JenkinsStatus PreviousCheckStatus { get; set; }

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
      var job = await this.api.GetJenkinsJob(this, cancellationToken);
      var latestBuild = await this.api.GetJenkinsBuild(this, job.LastBuild.Number, cancellationToken);
      this.CurrentStatus = CreateStatus(latestBuild);
      if (this.Snapshots.Count == 0 && this.MaxSnapshots > 0)
      {
        var startBuildNumber = latestBuild.Building ? latestBuild.Number - 1 : latestBuild.Number;
        var lastHistoryBuild = Math.Max(startBuildNumber - this.MaxSnapshots, 0);
        JenkinsStatus buildStatus = null;
        for (int i = lastHistoryBuild; i <= startBuildNumber; i++)
        {
          buildStatus = CreateStatus(await this.api.GetJenkinsBuild(this, i, cancellationToken));
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
      if (status is JenkinsStatus currentStatus)
      {
        if (this.PreviousCheckStatus != null)
        {
          if (status.State != ObservationState.Running && (status.State != this.PreviousCheckStatus.State || this.PreviousCheckStatus.BuildNumber != currentStatus.BuildNumber))
          {
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
      newStatus.Building = latestBuild.Building;
      newStatus.DurationInMs = latestBuild.Duration;
      newStatus.EstimatedDurationInMs = latestBuild.EstimatedDuration;
      newStatus.Culprits = latestBuild.Culprits;

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
  }
}
