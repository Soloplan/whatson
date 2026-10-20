// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsVolunteer.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins
{
  using System;
  using System.Net;
  using System.Text.RegularExpressions;

  public static class JenkinsVolunteer
  {
    private static readonly Regex Marker = new Regex(
      @"^\[whatsON\] Being fixed by ([^\r\n]+?) \[/whatsON\](?:\r?\n|$)",
      RegexOptions.Multiline | RegexOptions.CultureInvariant,
      TimeSpan.FromSeconds(1));

    public static string? GetOwner(string? description)
    {
      var match = Marker.Match(description ?? string.Empty);
      return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    public static string SetOwner(string? description, string? owner)
    {
      var text = description ?? string.Empty;
      if (Marker.Matches(text).Count > 1)
      {
        throw new InvalidOperationException("The build has multiple whatsON markers. Correct its description in Jenkins first.");
      }

      text = Marker.Replace(text, string.Empty);
      if (owner == null)
      {
        return text;
      }

      if (string.IsNullOrWhiteSpace(owner) || owner.IndexOfAny(['\r', '\n', '[', ']']) >= 0)
      {
        throw new ArgumentException("The Jenkins user name cannot be used as a volunteer marker.", nameof(owner));
      }

      return $"[whatsON] Being fixed by {WebUtility.HtmlEncode(owner)} [/whatsON]\n" + text;
    }
  }
}
