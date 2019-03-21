// // --------------------------------------------------------------------------------------------------------------------
namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.IO;
  using System.Net;
  using System.Threading;
  using System.Threading.Tasks;
  using Newtonsoft.Json;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.ServerBase;

  public static class JenkinsApi
  {
    public static async Task<JenkinsJob> GetJenkinsJob(JenkinsProject subject, CancellationToken token)
    {
      var address = subject.GetAddress();
      var port = subject.GetPort();
      var projectName = subject.GetProject();

      var jobRequest = $"{address}:{port}/job/{projectName}/api/json?tree={JenkinsJob.RequestProperties}";
      return await GetJenkinsModel<JenkinsJob>(subject, jobRequest, token);
    }

    public static async Task<JenkinsBuild> GetJenkinsBuild(JenkinsProject subject, int buildNumber, CancellationToken token)
    {
      var address = subject.GetAddress();
      var port = subject.GetPort();
      var projectName = subject.GetProject();

      var buildRequest = $"{address}:{port}/job/{projectName}/{buildNumber}/api/json?tree={JenkinsBuild.RequestProperties}";
      return await GetJenkinsModel<JenkinsBuild>(subject, buildRequest, token);
    }

    private static async Task<TModel> GetJenkinsModel<TModel>(JenkinsProject subject, string requestUrl, CancellationToken token)
    {
      var request = WebRequest.Create(requestUrl);
      using (token.Register(() => request.Abort(), false))
      using (var response = await request.GetResponseAsync())
      {
        try
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
        catch (WebException ex)
        {
          if (token.IsCancellationRequested)
          {
            throw new OperationCanceledException(ex.Message, ex, token);
          }

          throw;
        }
      }
    }
  }
}