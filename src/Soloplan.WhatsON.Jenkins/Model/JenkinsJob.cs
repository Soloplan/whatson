namespace Soloplan.WhatsON.Jenkins.Model
{
  using Newtonsoft.Json;

  public class JenkinsJob
  {
    public const string RequestProperties = "displayName,lastBuild[number]";

    public string DisplayName { get; set; }

    public JenkinsBuild LastBuild { get; set; }
  }
}
