namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.IO;
  using System.Net;

  [SubjectType("Jenkins Build Job Status", Description = "Retrieve the current status of a Jenkins build job.")]
  [ConfigurationItem(JobName, typeof(string))]
  public class JenkinsBuildJob : ServerSubject
  {
    public const string JobName = "JobName";

    public JenkinsBuildJob(string serverAdress, string jobName, string serverPort = null, string name = null)
      : base(name ?? jobName, serverAdress, serverPort)
    {
      this.Configuration[JobName] = jobName;
    }

    protected string Job => this.Configuration[JobName];

    protected override void ExecuteQuery(params string[] args)
    {
      // TODO: check out the available .NET wrapper NuGets for the Jenkins WebAPI
      // There are a lot of them, but none seems to be the standard way to go.
      // Maybe it's best to directly talk to the WebAPI because we possibly don't need a lot of functionality 
      WebRequest request = WebRequest.Create($"{this.Address}/job/{this.Job}/lastBuild/api/json?tree=result,timestamp,estimatedDuration");

      using (var response = (HttpWebResponse)request.GetResponse())
      {
        // Get the stream containing content returned by the server
        // Open the stream using a StreamReader for easy access
        using (var dataStream = response.GetResponseStream())
        using (var reader = new StreamReader(dataStream))
        {
          string responseFromServer = reader.ReadToEnd();

          Console.WriteLine(responseFromServer);

          // TODO: create Status from the JSON response
        }
      }
    }
  }
}
