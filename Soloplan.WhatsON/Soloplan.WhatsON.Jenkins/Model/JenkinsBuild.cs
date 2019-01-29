namespace Soloplan.WhatsON.Jenkins.Model
{
  using Newtonsoft.Json;

  public class JenkinsBuild
  {
    public const string RequestProperties = "number,displayName,description,building,duration,estimatedDuration,result,timestamp";

    public int Number { get; set; }

    public string DisplayName { get; set; }

    public string Description { get; set; }

    public bool Building { get; set; }

    public int Duration { get; set; }

    public int EstimatedDuration { get; set; }

    public string Result { get; set; }

    public long Timestamp { get; set; }
  }
}
