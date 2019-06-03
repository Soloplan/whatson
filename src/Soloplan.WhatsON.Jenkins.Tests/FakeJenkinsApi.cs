// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FakeJenkinsApi.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Jenkins.Model;

  public class FakeJenkinsApi : IJenkinsApi
  {
    public event EventHandler<JobEventArgs> JobRequest;

    public event EventHandler<BuildEventArgs> BuildRequest;

    public async Task<JenkinsJob> GetJenkinsJob(JenkinsProject connector, CancellationToken token)
    {
      if (this.JobRequest == null)
      {
        throw new InvalidOperationException($"{nameof(this.JobRequest)} must be handled for {nameof(FakeJenkinsApi)} to work.");
      }

      var eventArgs = new JobEventArgs(connector);
      this.JobRequest(this, eventArgs);
      if (eventArgs.ResponseDelay > 0)
      {
        await Task.Delay(eventArgs.ResponseDelay, token);
      }

      return eventArgs.Result;
    }

    public async Task<JenkinsBuild> GetJenkinsBuild(JenkinsProject connector, int buildNumber, CancellationToken token)
    {
      if (this.BuildRequest == null)
      {
        throw new InvalidOperationException($"{nameof(this.JobRequest)} must be handled for {nameof(FakeJenkinsApi)} to work.");
      }

      var eventArgs = new BuildEventArgs(connector, buildNumber);
      this.BuildRequest(this, eventArgs);
      if (eventArgs.ResponseDelay > 0)
      {
        await Task.Delay(eventArgs.ResponseDelay, token);
      }

      return eventArgs.Result;
    }
  }

  public class FakeApiEventArgs : EventArgs
  {
    public FakeApiEventArgs(JenkinsProject connector)
    {
      this.Connector = connector;
    }

    public JenkinsProject Connector { get; }

    public int ResponseDelay { get; set; }
  }

  public class JobEventArgs : FakeApiEventArgs
  {
    public JobEventArgs(JenkinsProject connector)
      : base(connector)
    {
    }

    public JenkinsJob Result { get; set; }
  }

  public class BuildEventArgs : FakeApiEventArgs
  {
    public BuildEventArgs(JenkinsProject connector, int buildNumber)
      : base(connector)
    {
      this.BuildNumber = buildNumber;
    }

    public int BuildNumber { get; }

    public JenkinsBuild Result { get; set; }
  }
}