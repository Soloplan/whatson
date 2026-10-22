// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsVolunteerPanel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using System.Net;
  using System.Text;
  using Newtonsoft.Json;

  public sealed record JenkinsFix(int BuildNumber, string Owner);

  public static class JenkinsVolunteerPanel
  {
    private const string Start = "<!-- whatsON:fixes:start -->";
    private const string End = "<!-- whatsON:fixes:end -->";
    private const string Metadata = "<!-- whatsON:fixes:data ";

    public static IReadOnlyList<JenkinsFix> GetFixes(string? description)
    {
      try
      {
        return Read(description ?? string.Empty);
      }
      catch (InvalidOperationException)
      {
        return Array.Empty<JenkinsFix>();
      }
    }

    public static string SetFix(string? description, int buildNumber, string? expectedOwner, string? owner)
    {
      var text = description ?? string.Empty;
      var fixes = Read(text).ToList();
      var existing = fixes.SingleOrDefault(fix => fix.BuildNumber == buildNumber);
      if (buildNumber <= 0 || (owner != null && (string.IsNullOrWhiteSpace(owner) || owner.Any(char.IsControl))))
      {
        throw new ArgumentException("Invalid Jenkins volunteer assignment.");
      }

      if (existing != null && existing.Owner != expectedOwner && existing.Owner != owner)
      {
        throw new InvalidOperationException("The volunteer has changed. Refresh the project before trying again.");
      }

      fixes.RemoveAll(fix => fix.BuildNumber == buildNumber);
      if (owner != null)
      {
        fixes.Add(new JenkinsFix(buildNumber, owner));
      }

      var start = text.IndexOf(Start, StringComparison.Ordinal);
      if (start >= 0)
      {
        var end = text.IndexOf(End, start, StringComparison.Ordinal) + End.Length;
        text = text.Remove(start, end - start);
      }

      if (fixes.Count == 0)
      {
        return text;
      }

      fixes.Sort((left, right) => left.BuildNumber.CompareTo(right.BuildNumber));
      var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(fixes)));
      var rows = fixes.Select(fix => "<li><strong>" + WebUtility.HtmlEncode(fix.Owner.ToUpperInvariant())
        + "</strong> — investigating failures since <a href=\"" + fix.BuildNumber.ToString(CultureInfo.InvariantCulture)
        + "/\">#" + fix.BuildNumber.ToString(CultureInfo.InvariantCulture) + "</a></li>");
      return Start + Metadata + encoded + " -->"
        + "<div style=\"border-left: 4px solid #3b82f6; padding: 10px 14px; margin: 8px 0 12px;\">"
        + "<h3 style=\"margin: 0 0 6px;\">Fixes in progress</h3><ul style=\"margin: 0; padding-left: 20px;\">"
        + string.Join(string.Empty, rows) + "</ul></div>" + End + text;
    }

    private static IReadOnlyList<JenkinsFix> Read(string text)
    {
      var start = text.IndexOf(Start, StringComparison.Ordinal);
      var end = text.IndexOf(End, StringComparison.Ordinal);
      if (start < 0 && end < 0 && !text.Contains(Metadata, StringComparison.Ordinal))
      {
        return Array.Empty<JenkinsFix>();
      }

      if (start < 0 || end < start)
      {
        throw new InvalidOperationException("The job has incomplete whatsON panel markers. Correct its description in Jenkins first.");
      }

      var metadataStart = start + Start.Length;
      var metadataEnd = text.IndexOf(" -->", metadataStart, StringComparison.Ordinal);
      if (start < 0 || end < metadataStart || metadataEnd < 0 || metadataEnd >= end
          || !text.AsSpan(metadataStart).StartsWith(Metadata, StringComparison.Ordinal)
          || text.Split(Start, StringSplitOptions.None).Length != 2
          || text.Split(End, StringSplitOptions.None).Length != 2
          || text.Split(Metadata, StringSplitOptions.None).Length != 2)
      {
        throw new InvalidOperationException("The job has damaged whatsON panel markers. Correct its description in Jenkins first.");
      }

      try
      {
        var offset = metadataStart + Metadata.Length;
        var json = new UTF8Encoding(false, true).GetString(Convert.FromBase64String(text.Substring(offset, metadataEnd - offset)));
        var fixes = JsonConvert.DeserializeObject<List<JenkinsFix?>>(json);
        if (fixes == null || fixes.Any(fix => fix == null || fix.BuildNumber <= 0 || string.IsNullOrWhiteSpace(fix.Owner) || fix.Owner.Any(char.IsControl))
            || fixes.OfType<JenkinsFix>().GroupBy(fix => fix.BuildNumber).Any(group => group.Count() > 1))
        {
          throw new InvalidOperationException("The job has invalid whatsON assignments. Correct its description in Jenkins first.");
        }

        return fixes.OfType<JenkinsFix>().ToArray();
      }
      catch (Exception exception) when (exception is FormatException or DecoderFallbackException or JsonException)
      {
        throw new InvalidOperationException("The job has damaged whatsON assignments. Correct its description in Jenkins first.", exception);
      }
    }
  }
}
