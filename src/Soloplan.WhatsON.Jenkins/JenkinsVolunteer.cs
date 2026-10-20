// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsVolunteer.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Linq;
  using System.Net;
  using System.Text;
  using System.Text.RegularExpressions;

  public static class JenkinsVolunteer
  {
    private const string Start = "<!-- whatsON:volunteer:start -->";
    private const string End = "<!-- whatsON:volunteer:end -->";
    private const string OwnerPrefix = "<!-- whatsON:volunteer:owner ";

    private static readonly Regex Marker = new Regex(
      Regex.Escape(Start) + @"\s*<!-- whatsON:volunteer:owner ([A-Za-z0-9+/]+={0,2}) -->[\s\S]*?" + Regex.Escape(End),
      RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
      TimeSpan.FromSeconds(1));

    private static readonly Regex LegacyMarker = new Regex(
      @"^\[whatsON\] Being fixed by ([^\r\n]+?) \[/whatsON\](?:\r?\n|$)",
      RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
      TimeSpan.FromSeconds(1));

    public static string? GetOwner(string? description)
    {
      try
      {
        return GetValidatedOwner(description);
      }
      catch (InvalidOperationException)
      {
        // A damaged notice must not break status display; writes use strict validation.
        return null;
      }
    }

    public static string SetOwner(string? description, string? owner)
    {
      var text = description ?? string.Empty;
      GetValidatedOwner(text);
      if (owner != null && (string.IsNullOrWhiteSpace(owner) || owner.Any(char.IsControl)))
      {
        throw new ArgumentException("The Jenkins user name cannot be used as a volunteer marker.", nameof(owner));
      }

      var replacement = owner == null ? string.Empty
        : Start + "\n" + OwnerPrefix + Convert.ToBase64String(new UTF8Encoding(false, true).GetBytes(owner))
          + " -->\n<hr />\n<div style=\"border-left: 4px solid #3b82f6; padding: 20px 24px;\">\n"
          + "<h3 style=\"font-size: 24px; font-weight: 600; margin: 8px 0 12px;\">Fix in progress</h3>\n"
          + "<p style=\"margin: 8px 0;\">Volunteer: <strong>" + WebUtility.HtmlEncode(owner) + "</strong></p>\n"
          + "</div>\n<hr />\n" + End;

      text = Marker.Replace(text, string.Empty);
      text = LegacyMarker.Replace(text, string.Empty);
      return text + replacement;
    }

    internal static string? GetValidatedOwner(string? description)
    {
      var text = description ?? string.Empty;
      var markers = Marker.Matches(text);
      var legacyMarkers = LegacyMarker.Matches(text);
      if (markers.Count + legacyMarkers.Count > 1
          || text.Split(Start, StringSplitOptions.None).Length - 1 != markers.Count
          || text.Split(End, StringSplitOptions.None).Length - 1 != markers.Count
          || text.Split(OwnerPrefix, StringSplitOptions.None).Length - 1 != markers.Count)
      {
        throw new InvalidOperationException("The build has duplicate or incomplete whatsON markers. Correct its description in Jenkins first.");
      }

      string? owner;
      try
      {
        owner = markers.Count == 1
          ? new UTF8Encoding(false, true).GetString(Convert.FromBase64String(markers[0].Groups[1].Value))
          : legacyMarkers.Count == 1 ? WebUtility.HtmlDecode(legacyMarkers[0].Groups[1].Value) : null;
      }
      catch (Exception exception) when (exception is FormatException or DecoderFallbackException)
      {
        throw new InvalidOperationException("The build has damaged whatsON owner metadata. Correct its description in Jenkins first.", exception);
      }

      if (owner != null && (string.IsNullOrWhiteSpace(owner) || owner.Any(char.IsControl)))
      {
        throw new InvalidOperationException("The build has invalid whatsON owner metadata. Correct its description in Jenkins first.");
      }

      return owner;
    }
  }
}
