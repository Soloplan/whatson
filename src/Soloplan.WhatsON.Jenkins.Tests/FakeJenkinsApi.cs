// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FakeJenkinsApi.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  public class FakeJenkinsApi : IJenkinsApi
  {
    public event EventHandler<JobEventArgs> JobRequest;

    public event EventHandler<BuildEventArgs> BuildRequest;

    public async Task<JenkinsJob> GetJenkinsJob(JenkinsConnector connector, CancellationToken token)
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

    public async Task<JenkinsBuild> GetJenkinsBuild(JenkinsConnector connector, int buildNumber, CancellationToken token)
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

    public Task<IList<JenkinsBuild>> GetBuilds(JenkinsConnector connector, CancellationToken token, int @from = 0, int to = Connector.MaxSnapshots)
    {
      throw new NotImplementedException();
    }
  }

  public class FakeApiEventArgs : EventArgs
  {
    public FakeApiEventArgs(JenkinsConnector connector)
    {
      this.Connector = connector;
    }

    public JenkinsConnector Connector { get; }

    public int ResponseDelay { get; set; }
  }

  public class JobEventArgs : FakeApiEventArgs
  {
    public JobEventArgs(JenkinsConnector connector)
      : base(connector)
    {
    }

    public JenkinsJob Result { get; set; }
  }

  public class BuildEventArgs : FakeApiEventArgs
  {
    public BuildEventArgs(JenkinsConnector connector, int buildNumber)
      : base(connector)
    {
      this.BuildNumber = buildNumber;
    }

    public int BuildNumber { get; }

    public JenkinsBuild Result { get; set; }
  }
}