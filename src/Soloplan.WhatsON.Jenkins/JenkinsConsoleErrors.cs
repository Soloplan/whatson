// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsConsoleErrors.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Text.RegularExpressions;
  using System.Threading;
  using System.Threading.Tasks;

  internal static class JenkinsConsoleErrors
  {
    private static readonly Regex diagnostic = new Regex(@"(?:^|[/\\])(?<file>[^/\\:]+(?::\d+(?::\d+)?)?)\s*:\s*(?<severity>(?:fatal\s+)?error|warning|note)(?:\s+(?<code>[A-Z]+\d+))?\s*:\s*(?<message>.+?)(?:\s+\[[^\]]+\])?$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    private static readonly Regex timestamp = new Regex(@"^\s*(?:\[(?:\d{4}[-/]\d{2}[-/]\d{2}[T ][^\]]+|\d{2}:\d{2}:\d{2}[^\]]*)\]|\d{4}[-/]\d{2}[-/]\d{2}[T ][\d:.Z+-]+)\s*", RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    private static readonly Regex logLevel = new Regex(@"^(?:[\w.$/-]+\s+(?:[-:]\s*)?)?(?<level>INFO|DEBUG|TRACE|WARN(?:ING)?|FINE(?:R|ST)?|NOTE|NOTICE|VERBOSE|ERROR|FATAL|SEVERE|ERR!?)(?:\[[A-Z]+\d+\])?(?:\s*:\s*|\s+|$)", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    private static readonly Regex noise = new Regex(@"^(?:Failed in branch\b|Stage\b.*\bskipped\b|Skipping stage\b|Build FAILED\.?$|Finished:\s*(?:FAILURE|UNSTABLE)\b|\d+ (?:Error|Warning)\(s\)|(?:\+\s*)?(?:echo|printf)\s)|\berror MSB(?:3073|6006)\b", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    private static readonly Regex searchPattern = new Regex(@"^(?:(?:try(?:ing)? to find|looking for|search(?:ing)? for) errors?\b|(?:pattern|regex|regexp)\s*[:=])", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    private static readonly Regex exitCode = new Regex(@"^(?:(?:ERROR|FATAL|[\w.]+Exception)\s*:\s*)?(?:script returned exit code\b|(?:process|command|\S+\.exe) exited with (?:error )?code\b)", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    private static readonly Regex exceptionDetail = new Regex(@"\b(?:[\w$]+\.)*(?:[\w$]*Exception|[\w$]+Error)\s*:\s*\S|\bunhandled exception\b", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    private static readonly Regex failure = new Regex(@"\b(?:fail(?:ed|ure)?|fatal|aborted|crashed|cannot|unable to|could not|access denied|permission denied|not found|timed out|refused|missing|no tests? (?:is |were |was |are )?(?:available|found|discovered|run|executed))\b", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    private static readonly Regex testFailure = new Regex(@"^(?:Failed|Skipped|Not run)\s+\S|\[(?:FAIL|SKIP)\]|\btest(?:s|host)?\b|\btest run\b", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    private static readonly Regex emptyFailureCount = new Regex(@"\b(?:failed|failures?|errors?)\s*:\s*0\b", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    private static readonly Regex detailHeader = new Regex(@"^(?:Error Message|Error|Exception|Caused by)\s*:\s*$", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    public static async Task<string> ReadAsync(TextReader reader, CancellationToken token, bool expanded = false)
    {
      var errors = new List<(int Priority, string Text)>();
      var line = new StringBuilder();
      var buffer = new char[8192];
      string? pendingDetail = null;
      int count;

      // Scan through the end: warning-heavy builds often print their useful diagnostics last.
      // shortcut: retain 8K per line; increase this if tools emit longer diagnostic lines.
      while ((count = await reader.ReadAsync(buffer.AsMemory(), token)) > 0)
      {
        for (var index = 0; index < count; index++)
        {
          if (buffer[index] == '\n')
          {
            pendingDetail = AddLine(line.ToString(), errors, pendingDetail, expanded);
            line.Clear();
          }
          else if (line.Length < 8192)
          {
            line.Append(buffer[index]);
          }
        }
      }

      AddLine(line.ToString(), errors, pendingDetail, expanded);

      return string.Join(Environment.NewLine, errors.Select(error => error.Text));
    }

    private static string? AddLine(string line, List<(int Priority, string Text)> errors, string? pendingDetail, bool expanded)
    {
      line = Regex.Replace(line, @"\x1b\[[0-9;]*[A-Za-z]", string.Empty);
      line = timestamp.Replace(line, string.Empty).Trim();
      if (line.Length == 0)
      {
        return pendingDetail;
      }

      var message = line;
      var errorLevel = false;
      while (message.StartsWith('['))
      {
        var end = message.IndexOf(']');
        if (end <= 0)
        {
          break;
        }

        var tag = message.Substring(1, end - 1).Trim();
        if (tag.Equals("Pipeline", StringComparison.OrdinalIgnoreCase))
        {
          return null;
        }

        var tagLevel = logLevel.Match(tag);
        if (tagLevel.Success)
        {
          if (!IsErrorLevel(tagLevel.Groups["level"].Value) && !errorLevel)
          {
            return null;
          }

          errorLevel = true;
        }

        message = message.Substring(end + 1).TrimStart();
      }

      var level = logLevel.Match(message);
      if (level.Success && !errorLevel)
      {
        if (!IsErrorLevel(level.Groups["level"].Value))
        {
          return null;
        }

        errorLevel = true;
      }

      if (noise.IsMatch(message) || exitCode.IsMatch(message) || !errorLevel && searchPattern.IsMatch(message))
      {
        return null;
      }

      var match = diagnostic.Match(message);
      if (match.Success)
      {
        if (!match.Groups["severity"].Value.EndsWith("error", StringComparison.OrdinalIgnoreCase))
        {
          return null;
        }

        var detail = match.Groups["message"].Value.Split(" and no accessible extension method", StringSplitOptions.None)[0];
        var file = match.Groups["file"].Value.Trim();
        var code = match.Groups["code"].Success ? match.Groups["code"].Value : "error";
        AddDistinct(errors, $"{file}: {code}: {detail}", 5, expanded);
      }
      else if (detailHeader.IsMatch(message))
      {
        return message.TrimEnd(':', ' ');
      }
      else if (pendingDetail != null)
      {
        AddDistinct(errors, $"{pendingDetail}: {message}", 4, expanded);
      }
      else if (exceptionDetail.IsMatch(message))
      {
        AddDistinct(errors, line, 4, expanded);
      }
      else if (emptyFailureCount.IsMatch(message))
      {
        return null;
      }
      else if (failure.IsMatch(message) || testFailure.IsMatch(message) && Regex.IsMatch(message, @"\b(?:Skipped|Ignored|Not run|Not executed|Did not run)\b|\[(?:FAIL|SKIP)\]", RegexOptions.IgnoreCase))
      {
        AddDistinct(errors, line, testFailure.IsMatch(message) ? 4 : errorLevel ? 3 : 2, expanded);
      }
      else if (errorLevel)
      {
        AddDistinct(errors, line, 3, expanded);
      }

      return null;
    }

    private static bool IsErrorLevel(string level) => level.Equals("ERROR", StringComparison.OrdinalIgnoreCase)
      || level.Equals("FATAL", StringComparison.OrdinalIgnoreCase) || level.Equals("SEVERE", StringComparison.OrdinalIgnoreCase)
      || level.TrimEnd('!').Equals("ERR", StringComparison.OrdinalIgnoreCase);

    private static void AddDistinct(List<(int Priority, string Text)> errors, string line, int priority, bool expanded)
    {
      line = Regex.Replace(line, @"\s+", " ").Trim();
      if (!expanded && line.Length > 160)
      {
        line = line.Substring(0, 157) + "...";
      }

      if (errors.Any(error => error.Text == line))
      {
        return;
      }

      var position = errors.FindIndex(error => error.Priority < priority);
      errors.Insert(position < 0 ? errors.Count : position, (priority, line));
      // shortcut: retain 1000 expanded errors; larger logs remain available on the build page.
      var limit = expanded ? 1000 : 5;
      if (errors.Count > limit)
      {
        errors.RemoveAt(limit);
      }
    }
  }
}
