// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuildParsingTests.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.CruiseControl.Tests
{
  using System;
  using HtmlAgilityPack;
  using NUnit.Framework;
  using Soloplan.WhatsON.CruiseControl.Model;

  [TestFixture]
  [SetCulture("en-US")]
  public class BuildParsingTests
  {
    [TestCase("build-passed-link", CcBuildStatus.Success)]
    [TestCase("build-failed-link", CcBuildStatus.Failure)]
    [TestCase("build-pending-link", CcBuildStatus.Unknown)]
    public void RecentBuildLinksPreserveResultNumberTimestampAndReportUrl(string cssClass, CcBuildStatus expectedStatus)
    {
      var node = HtmlNode.CreateNode($"<a href='/ccnet/server/local/project/Release/build/log20201017120000L42.xml' class='{cssClass}'>10/17/2020 12:00:00 PM (42)</a>");

      var build = CruiseControlBuild.FromHtmlNode(node, "https://builds.example");

      Assert.Multiple(() =>
      {
        Assert.That(build.Status, Is.EqualTo(expectedStatus));
        Assert.That(build.BuildNumber, Is.EqualTo(42));
        Assert.That(build.BuildLabel, Is.EqualTo("#42"));
        Assert.That(build.BuildTime, Is.EqualTo(new DateTime(2020, 10, 17, 12, 0, 0)));
        Assert.That(build.Name, Is.EqualTo("log20201017120000L42.xml"));
        Assert.That(build.Url, Is.EqualTo("https://builds.example/ccnet/server/local/project/Release/build/log20201017120000L42.xml"));
      });
    }

    [Test]
    public void NonNumericBuildLabelsAreNotReplacedWithZero()
    {
      var node = HtmlNode.CreateNode("<a href='/project/Release/build/release.xml' class='passed'>10/17/2020 12:00:00 PM (release_2020)</a>");

      var build = CruiseControlBuild.FromHtmlNode(node, "https://builds.example");

      Assert.That(build.BuildLabel, Is.EqualTo("release_2020"));
      Assert.That(build.BuildNumber, Is.Zero);
    }

    [Test]
    public void EscapedBuildLogNamesAreDecodedForDisplay()
    {
      var node = HtmlNode.CreateNode("<a href='/project/Release/build/build%20log.xml' class='passed'>10/17/2020 12:00:00 PM (42)</a>");

      var build = CruiseControlBuild.FromHtmlNode(node, "https://builds.example");

      Assert.That(build.Name, Is.EqualTo("build log.xml"));
      Assert.That(build.Url, Does.EndWith("build%20log.xml"));
    }

    [Test]
    public void InvalidBuildTimesAreReportedInsteadOfInventingATimestamp()
    {
      var node = HtmlNode.CreateNode("<a href='/project/Release/build/build.xml' class='passed'>not-a-date (42)</a>");

      Assert.That(() => CruiseControlBuild.FromHtmlNode(node, "https://builds.example"), Throws.TypeOf<FormatException>());
    }

    [TestCase("https://builds.example/ccnet", "Release 1", "https://builds.example/ccnet/project/Release%201/ViewProjectReport.aspx")]
    [TestCase("https://builds.example/ccnet///", "A/B #1?", "https://builds.example/ccnet/project/A%2FB%20%231%3F/ViewProjectReport.aspx")]
    [TestCase("https://builds.example/ccnet/", null, "https://builds.example/ccnet")]
    public void ProjectLinksPreserveServerPathAndEscapeProjectNames(string server, string project, string expected)
    {
      Assert.That(CruiseControlServer.UrlHelper.GetReportUrl(server, project), Is.EqualTo(expected));
    }

    [Test]
    public void StatusAndHistoryUrlsUseTheServerPathWithoutDuplicateSlashes()
    {
      Assert.That(CruiseControlServer.UrlHelper.GetXmlReportUrl("https://builds.example/ccnet///"), Is.EqualTo("https://builds.example/ccnet/XmlStatusReport.aspx"));
      Assert.That(CruiseControlServer.UrlHelper.GetAllBuildsUrl("https://builds.example/ccnet///", "Release #1"), Is.EqualTo("https://builds.example/ccnet/project/Release%20%231/ViewAllBuilds.aspx"));
    }
  }
}
