namespace Soloplan.WhatsON.CruiseControl.Model
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;
  using System.Xml.Linq;
  using System.Xml.XPath;
  using ThoughtWorks.CruiseControl.Core;
  using ThoughtWorks.CruiseControl.Remote;

  public class CruiseControlBuild
  {
    public string Id { get; set; }

    public CcBuildStatus Status { get; set; }

    public int BuildNumber { get; set; }

    public TimeSpan Duration { get; set; }

    public DateTime BuildTime { get; set; }

    public List<CruiseControlUser> Culprits { get; set; }

    public static CruiseControlBuild FromRawLog(ThoughtWorks.CruiseControl.Remote.CruiseServerClient client, string log)
    {
      var logXml = XDocument.Parse(log);
      var id = logXml.XPathSelectElement($"/cruisecontrol/integrationProperties/{IntegrationPropertyNames.CCNetBuildId}")?.Value;
      var time = logXml.XPathSelectElement($"/cruisecontrol/integrationProperties/{IntegrationPropertyNames.CCNetBuildTime}")?.Value;
      var projectName = logXml.XPathSelectElement($"/cruisecontrol/integrationProperties/{IntegrationPropertyNames.CCNetLabel}")?.Value;
      var build = logXml.XPathSelectElement($"/cruisecontrol/integrationProperties/{IntegrationPropertyNames.CCNetNumericLabel}")?.Value;
      var status = logXml.XPathSelectElement($"/cruisecontrol/integrationProperties/{IntegrationPropertyNames.CCNetIntegrationStatus}")?.Value;
      var duration = logXml.XPathSelectElement($"/cruisecontrol/build")?.Attribute("buildtime")?.Value;

      var buildTime = DateTime.Parse(time);
      var buildNumber = int.TryParse(build, out var b) ? b : 0;
      var buildDuration = TimeSpan.Parse(duration);
      var buildStatus = (CcBuildStatus)Enum.Parse(typeof(CcBuildStatus), status);
      return new CruiseControlBuild
      {
        Id = id,
        BuildNumber = buildNumber,
        BuildTime = buildTime,
        Duration = buildDuration,
        Status = buildStatus,
      };
    }
  }
}
