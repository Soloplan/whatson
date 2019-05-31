namespace Soloplan.WhatsON.Jenkins.Model
{
  using System.Collections.Generic;
  using Newtonsoft.Json;

  public class JenkinsBuild
  {
    public const string RequestProperties = "number,displayName,description,building,duration,estimatedDuration,result,timestamp,culprits[fullName,absoluteUrl],changeSets[items[author[fullName,absoluteUrl]]]";

    public int Number { get; set; }

    public string DisplayName { get; set; }

    public string Description { get; set; }

    public bool Building { get; set; }

    public int Duration { get; set; }

    public int EstimatedDuration { get; set; }

    public string Result { get; set; }

    public long Timestamp { get; set; }

    public IList<Culprit> Culprits { get; set; }

    public IList<ChangeSet> ChangeSets { get; set; }
  }
}
