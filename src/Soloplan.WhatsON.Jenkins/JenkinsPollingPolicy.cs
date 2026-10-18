// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsPollingPolicy.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Linq;
  using Soloplan.WhatsON.Jenkins.Model;

  internal sealed class JenkinsPollingPolicy
  {
    internal static readonly TimeSpan MaximumInterval = TimeSpan.FromMinutes(5);

    private int failures;

    public DateTimeOffset NextDue { get; private set; } = DateTimeOffset.MinValue;

    public bool Priority { get; private set; }

    public void Succeeded(JenkinsJob? job, DateTimeOffset now)
    {
      this.failures = 0;
      var seconds = 60.0;
      if (job?.InQueue == true)
      {
        seconds = 10;
      }
      else if (job?.Disabled == true && job.LastBuild?.Building != true)
      {
        seconds = MaximumInterval.TotalSeconds;
      }
      else if (job?.LastBuild != null)
      {
        var build = job.LastBuild;
        var started = DateTimeOffset.FromUnixTimeMilliseconds(build.Timestamp);
        var history = (job.Builds ?? Array.Empty<JenkinsBuild>()).Where(item => item.Timestamp > 0).OrderByDescending(item => item.Timestamp).ToList();
        var durations = history.Where(item => !item.Building && item.Duration > 0).Select(item => (double)item.Duration).ToList();
        var average = durations.Count > 0 ? durations.Average() : build.Duration;
        var duration = build.Building && build.EstimatedDuration > 0 ? build.EstimatedDuration : average > 0 ? average : build.EstimatedDuration;
        if (build.Building)
        {
          var remaining = started.AddMilliseconds(Math.Max(0, duration)) - now;
          seconds = remaining > TimeSpan.FromMinutes(2) ? 30 : 10;
        }
        else
        {
          var idle = now - started.AddMilliseconds(Math.Max(0, build.Duration));
          seconds = idle < TimeSpan.FromMinutes(2) ? 10 : idle < TimeSpan.FromMinutes(15) ? 30 : idle < TimeSpan.FromDays(1) ? 60 : 300;
          if (duration > 0 && duration <= 30000 && idle < TimeSpan.FromMinutes(30))
          {
            seconds = 10;
          }

          var gaps = history.Zip(history.Skip(1), (newer, older) => (double)(newer.Timestamp - older.Timestamp)).Where(gap => gap > 0).ToList();
          if (gaps.Count > 0)
          {
            var expected = started.AddMilliseconds(gaps.Average());
            if (Math.Abs((expected - now).TotalSeconds) <= 60)
            {
              seconds = 10;
            }
          }
        }
      }

      this.Priority = seconds <= 30;
      this.NextDue = now.AddSeconds(seconds);
    }

    public void Failed(DateTimeOffset now)
    {
      this.failures = Math.Min(this.failures + 1, 6);
      this.Priority = false;
      this.NextDue = now.AddSeconds(Math.Min(MaximumInterval.TotalSeconds, 10 * Math.Pow(2, this.failures - 1)));
    }
  }
}
