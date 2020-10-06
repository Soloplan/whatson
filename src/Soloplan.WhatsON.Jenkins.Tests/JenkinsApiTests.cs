// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsApiTests.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using NUnit.Framework;
  using Soloplan.WhatsON;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class JenkinsApiTests
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
           e.Result = ApiHelper.GetProject(ObservationState.Running, 10);
           state++;
         }
         else if (state == 1)
         {
           e.Result = ApiHelper.GetProject(ObservationState.Running, 11);
           state++;
         }
         else
         {
           e.Result = ApiHelper.GetProject(ObservationState.Running, 12);
           state++;
           scheduler.Stop(false);
         }
       };

      api.BuildsRequest += (s, e) =>
      {
        var buildsStates = new Dictionary<int, ObservationState>
        {
          { 11, ObservationState.Failure },
          { 10, ObservationState.Success },
          { 9, ObservationState.Unstable },
          { 8, ObservationState.Unstable },
          { 7, ObservationState.Unstable },
          { 6, ObservationState.Unstable },
          { 5, ObservationState.Unstable },
          { 4, ObservationState.Unstable },
        };

        int start = 0;
        int end = 0;

        start = 8 + state;
        end = 4 + state;

        for (int i = start; i >= end; i--)
        {
          e.Builds.Add(ApiHelper.GetBuild(buildsStates[i], i));
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

      var subj = new JenkinsConnector(new ConnectorConfiguration("Jenkins", "name", "Address", "MyAddress", "ProjectName", "MyProject"), api);
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