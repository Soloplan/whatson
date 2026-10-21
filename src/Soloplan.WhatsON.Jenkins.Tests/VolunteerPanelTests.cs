// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VolunteerPanelTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using System.Linq;
  using NUnit.Framework;

  [TestFixture]
  public class VolunteerPanelTests
  {
    [TestCase(null)]
    [TestCase("")]
    [TestCase("<p>Original job description</p>\r\n")]
    public void PanelKeepsMultipleAssignmentsAndRestoresTheOriginalDescription(string? original)
    {
      var first = JenkinsVolunteerPanel.SetFix(original, 34, null, "rHr");
      var second = JenkinsVolunteerPanel.SetFix(first, 37, null, "Alex & team");
      Assert.That(second, Does.Contain("Fixes in progress"));
      Assert.That(second, Does.Contain("<strong>RHR</strong>"));
      Assert.That(second, Does.Contain("ALEX &amp; TEAM"));
      Assert.That(second, Does.Contain("href=\"34/\""));
      Assert.That(second.EndsWith(original ?? string.Empty, StringComparison.Ordinal), Is.True);
      Assert.That(JenkinsVolunteerPanel.GetFixes(second), Is.EqualTo(new[] { new JenkinsFix(34, "rHr"), new JenkinsFix(37, "Alex & team") }));

      var removed = JenkinsVolunteerPanel.SetFix(second, 34, "rHr", null);
      Assert.That(JenkinsVolunteerPanel.GetFixes(removed).Single(), Is.EqualTo(new JenkinsFix(37, "Alex & team")));
      Assert.That(JenkinsVolunteerPanel.SetFix(removed, 37, "Alex & team", null), Is.EqualTo(original ?? string.Empty));
    }

    [Test]
    public void RepeatedUpdatesAreIdempotentAndOtherOwnersCannotBeReplaced()
    {
      var panel = JenkinsVolunteerPanel.SetFix("Notes", 34, null, "alex");
      Assert.That(JenkinsVolunteerPanel.SetFix(panel, 34, null, "alex"), Is.EqualTo(panel));
      Assert.Throws<InvalidOperationException>(() => JenkinsVolunteerPanel.SetFix(panel, 34, null, "sam"));
      Assert.Throws<InvalidOperationException>(() => JenkinsVolunteerPanel.SetFix(panel, 34, "sam", null));
    }

    [TestCase("<!-- whatsON:fixes:end -->")]
    [TestCase("<!-- whatsON:fixes:start -->")]
    [TestCase("<!-- whatsON:fixes:start --><!-- whatsON:fixes:data /w== --><!-- whatsON:fixes:end -->")]
    [TestCase("<!-- whatsON:fixes:start --><!-- whatsON:fixes:data bnVsbA== --><!-- whatsON:fixes:end -->")]
    public void DamagedPanelsCannotBeEditedAndDoNotBreakDisplay(string description)
    {
      Assert.That(JenkinsVolunteerPanel.GetFixes(description), Is.Empty);
      Assert.Throws<InvalidOperationException>(() => JenkinsVolunteerPanel.SetFix(description, 34, null, "alex"));
    }

    [Test]
    public void DuplicatePanelsCannotBeEdited()
    {
      var panel = JenkinsVolunteerPanel.SetFix(null, 34, null, "alex");
      Assert.Throws<InvalidOperationException>(() => JenkinsVolunteerPanel.SetFix(panel + panel, 35, null, "sam"));
    }
  }
}
