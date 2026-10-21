// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FakeJenkinsApi.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  public class FakeJenkinsApi : IJenkinsApi
  {
    public event EventHandler<JobEventArgs>? JobRequest;

    public event EventHandler<BuildEventArgs>? BuildRequest;

    public event EventHandler<BuildsEventArgs>? BuildsRequest;

    public Func<int, CancellationToken, Task<string?>> FailedTestsRequest { get; set; } = (_, _) => Task.FromResult<string?>(null);

    public Func<int, CancellationToken, Task<string?>> ConsoleErrorsRequest { get; set; } = (_, _) => Task.FromResult<string?>(string.Empty);

    public Func<int, CancellationToken, Task<string?>> PipelineFailureRequest { get; set; } = (_, _) => Task.FromResult<string?>(null);

    public Func<IReadOnlyCollection<JenkinsConnector>, CancellationToken, Task<JenkinsPollingResult>>? PollingQuery { get; set; }

    public Func<JenkinsConnector, int, string?, bool, CancellationToken, Task<JenkinsBuild>>? VolunteerQuery { get; set; }

    public Func<JenkinsConnector, int, string?, string?, CancellationToken, Task<string>>? JobVolunteerQuery { get; set; }

    public async Task<string> SetJobVolunteer(JenkinsConnector connector, int buildNumber, string? expectedOwner, string? owner, CancellationToken token)
    {
      if (this.JobVolunteerQuery != null)
      {
        return await this.JobVolunteerQuery(connector, buildNumber, expectedOwner, owner, token);
      }

      var job = await this.GetJenkinsJob(connector, token) ?? throw new InvalidOperationException("Missing job.");
      job.Description = JenkinsVolunteerPanel.SetFix(job.Description, buildNumber, expectedOwner, owner);
      return job.Description;
    }

    public Task<string?> GetFailedTests(JenkinsConnector connector, int buildNumber, CancellationToken token) => this.FailedTestsRequest(buildNumber, token);

    public Task<string?> GetConsoleErrors(JenkinsConnector connector, int buildNumber, CancellationToken token) => this.ConsoleErrorsRequest(buildNumber, token);

    public Task<string?> GetPipelineFailure(JenkinsConnector connector, int buildNumber, CancellationToken token) => this.PipelineFailureRequest(buildNumber, token);

    public Task<JenkinsBuild> SetVolunteer(JenkinsConnector connector, int buildNumber, string? expectedOwner, bool claim, CancellationToken token) =>
      this.VolunteerQuery?.Invoke(connector, buildNumber, expectedOwner, claim, token) ?? throw new NotSupportedException();

    public Task<JenkinsPollingResult> GetPollingJobs(IReadOnlyCollection<JenkinsConnector> connectors, CancellationToken token) =>
      this.PollingQuery?.Invoke(connectors, token) ?? throw new InvalidOperationException($"{nameof(this.PollingQuery)} must be set for server batching.");

    public async Task<JenkinsJob?> GetJenkinsJob(JenkinsConnector connector, CancellationToken token)
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

    public async Task<JenkinsBuild?> GetJenkinsBuild(JenkinsConnector connector, int buildNumber, CancellationToken token)
    {
      if (this.BuildRequest == null)
      {
        throw new InvalidOperationException($"{nameof(this.BuildRequest)} must be handled for {nameof(FakeJenkinsApi)} to work.");
      }

      var eventArgs = new BuildEventArgs(connector, buildNumber);
      this.BuildRequest(this, eventArgs);
      if (eventArgs.ResponseDelay > 0)
      {
        await Task.Delay(eventArgs.ResponseDelay, token);
      }

      return eventArgs.Result;
    }

    public async Task<IList<JenkinsBuild>> GetBuilds(
      JenkinsConnector connector,
      CancellationToken token,
      int from = 0,
      int to = Connector.MaxSnapshots)
    {
      if (this.BuildsRequest == null)
      {
        throw new InvalidOperationException($"{nameof(this.BuildsRequest)} must be handled for {nameof(FakeJenkinsApi)} to work.");
      }

      var eventArgs = new BuildsEventArgs(connector, from, to);
      this.BuildsRequest(this, eventArgs);
      if (eventArgs.ResponseDelay > 0)
      {
        await Task.Delay(eventArgs.ResponseDelay, token);
      }

      return eventArgs.Builds;
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

    public JenkinsJob? Result { get; set; }
  }

  public class BuildEventArgs : FakeApiEventArgs
  {
    public BuildEventArgs(JenkinsConnector connector, int buildNumber)
      : base(connector)
    {
      this.BuildNumber = buildNumber;
    }

    public int BuildNumber { get; }

    public JenkinsBuild? Result { get; set; }
  }

  public class BuildsEventArgs : FakeApiEventArgs
  {
    public BuildsEventArgs(JenkinsConnector connector, int from, int to)
      : base(connector)
    {
      this.From = from;
      this.To = to;
      this.Builds = new List<JenkinsBuild>();
    }

    public int From { get; }

    public int To { get; }

    public IList<JenkinsBuild> Builds { get; }
  }
}
