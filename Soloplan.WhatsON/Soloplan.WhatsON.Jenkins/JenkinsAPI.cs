// // --------------------------------------------------------------------------------------------------------------------
namespace Soloplan.WhatsON.Jenkins
{
  using System.IO;
  using System.Net;
  using Newtonsoft.Json;
  using Soloplan.WhatsON.Jenkins.Model;

  public static class JenkinsApi
  {
    public static JenkinsJob GetJenkinsJob(JenkinsProject subject)
    {
      var address = subject.GetAddress();
      var port = subject.GetPort();
      var projectName = subject.GetProject();

      var jobRequest = $"{address}:{port}/job/{projectName}/api/json?tree={JenkinsJob.RequestProperties}";
      return GetJenkinsModel<JenkinsJob>(subject, jobRequest);
    }

    public static JenkinsBuild GetJenkinsBuild(JenkinsProject subject, int buildNumber)
    {
      var address = subject.GetAddress();
      var port = subject.GetPort();
      var projectName = subject.GetProject();

      var buildRequest = $"{address}:{port}/job/{projectName}/{buildNumber}/api/json?tree={JenkinsBuild.RequestProperties}";
      return GetJenkinsModel<JenkinsBuild>(subject, buildRequest);
    }

    private static TModel GetJenkinsModel<TModel>(JenkinsProject subject, string requestUrl)
    {
      var request = WebRequest.Create(requestUrl);
      using (var response = (HttpWebResponse)request.GetResponse())
      {
        // Get the stream containing content returned by the server
        // Open the stream using a StreamReader for easy access
        using (var dataStream = response.GetResponseStream())
        using (var reader = new StreamReader(dataStream))
        {
          var responseFromServer = reader.ReadToEnd();
          return JsonConvert.DeserializeObject<TModel>(responseFromServer);
        }
      }
    }
  }
}