// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CruiseControlBuild.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.Model
{
  using System;
  using System.Text.RegularExpressions;
  using HtmlAgilityPack;

  public class CruiseControlBuild
  {
    public string Name { get; set; }

    public int BuildNumber { get; set; }

    public string BuildLabel { get; set; }

    public CcBuildStatus Status { get; set; }

    public DateTime BuildTime { get; set; }

    public string Url { get; set; }

    public static CruiseControlBuild FromHtmlNode(HtmlNode node, string serverUrl)
    {
      var url = node.Attributes["href"].Value;
      var clazz = node.Attributes["class"].Value;
      var value = node.InnerHtml;

      var labelMatch = Regex.Match(value, @"\((\w*)\)");
      var label = labelMatch.Groups[1].Value;

      var timeMatch = Regex.Match(value, @"(.*) \((\w*)\)");
      var time = DateTime.Parse(timeMatch.Groups[1].Value);

      var nameMatch = Regex.Match(url, @".*/project/(.*)/build/(.*\.xml)");
      var name = Uri.UnescapeDataString(nameMatch.Groups[2].Value);

      var status = clazz.Contains("failed") ? CcBuildStatus.Failure : clazz.Contains("passed") ? CcBuildStatus.Success : CcBuildStatus.Unknown;
      var build = new CruiseControlBuild
      {
        Url = $"{serverUrl}{url}",
        BuildLabel = label,
        BuildTime = time,
        Name = name,
        Status = status,
      };

      if (int.TryParse(label, out var buildNumber))
      {
        build.BuildNumber = buildNumber;
        build.BuildLabel = $"#{build.BuildNumber}";
      }

      return build;
    }
  }
}
