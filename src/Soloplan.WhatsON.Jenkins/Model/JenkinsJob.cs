namespace Soloplan.WhatsON.Jenkins.Model
{
  using Newtonsoft.Json;

  public class JenkinsJob
  {
    public const string RequestProperties = "displayName,lastBuild[number],firstBuild[number]";

    public string DisplayName { get; set; }

    public JenkinsBuild LastBuild { get; set; }

    public JenkinsBuild FirstBuild { get; set; }
  }
}
