// <copyright file="JenkinsApiTest.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using System.Linq;
  using NUnit.Framework;
  using Soloplan.WhatsON;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Jenkins.Model;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class JenkinsApiTest
  {
    [Test]
    public void BuildsShouldNotBeMissedWhenBuildsAreRunningOneAfterAnother()
    {
      var scheduler = new ObservationScheduler();
      var api = new FakeJenkinsApi();

      int state = 0;
      api.JobRequest += (s, e) =>
       {
         if (state == 0)
         {
           e.Result = ApiHelper.GetProject(ObservationState.Running, 10, 7);
           state++;
         }
         else if (state == 1)
         {
           e.Result = ApiHelper.GetProject(ObservationState.Running, 11, 7);
           state++;
         }
         else
         {
           e.Result = ApiHelper.GetProject(ObservationState.Running, 12, 7);
           scheduler.Stop(false);
         }
       };

      api.BuildRequest += (s, e) =>
      {
        if (e.BuildNumber < 10)
        {
          e.Result = ApiHelper.GetBuild(ObservationState.Unstable, e.BuildNumber);
        }
        else if (state == 0)
        {
          e.Result = ApiHelper.GetBuild(ObservationState.Running, e.BuildNumber);
        }
        else if (state == 1)
        {
          var buildSt = e.BuildNumber == 10 ? ObservationState.Success : ObservationState.Running;
          e.Result = ApiHelper.GetBuild(buildSt, e.BuildNumber);
        }
        else
        {
          ObservationState buildSt;
          if (e.BuildNumber == 10)
          {
            buildSt = ObservationState.Success;
          }
          else if (e.BuildNumber == 11)
          {
            buildSt = ObservationState.Failure;
          }
          else
          {
            buildSt = ObservationState.Running;
          }

          e.Result = ApiHelper.GetBuild(buildSt, e.BuildNumber);
        }
      };

      var subj = new JenkinsConnector(new ConnectorConfiguration(nameof(JenkinsConnector)), api);
      scheduler.Observe(subj, 0);
      scheduler.Start();
      while (scheduler.Running)
      {
      }

      var jenkinsStatuses = subj.Snapshots.Select(snap => snap.Status).OfType<JenkinsStatus>().ToList();
      Assert.That(jenkinsStatuses.Count, Is.EqualTo(5));

      var expectedBuilds = new[]
      {
        new Tuple<int, ObservationState>(11, ObservationState.Failure),
        new Tuple<int, ObservationState>(10, ObservationState.Success),
        new Tuple<int, ObservationState>(9, ObservationState.Unstable),
        new Tuple<int, ObservationState>(8, ObservationState.Unstable),
        new Tuple<int, ObservationState>(7, ObservationState.Unstable),
      };

      foreach (var expectedBuild in expectedBuilds)
      {
        Assert.That(jenkinsStatuses.Any(stat => stat.BuildNumber == expectedBuild.Item1 && stat.State == expectedBuild.Item2), Is.True, $"Build No. {expectedBuild} was not present in snapshots.");
      }
    }
  }
}