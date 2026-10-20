// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VolunteerNoticeTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Jenkins.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net;
  using NUnit.Framework;

  [TestFixture]
  public class VolunteerNoticeTests
  {
    private const string Start = "<!-- whatsON:volunteer:start -->";
    private const string End = "<!-- whatsON:volunteer:end -->";
    private const string Owner = "<!-- whatsON:volunteer:owner YWxleA== -->";

    [TestCaseSource(nameof(InvalidNotices))]
    public void DamagedOrAmbiguousNoticesNeverIdentifyAnOwnerOrAllowWrites(string description)
    {
      Assert.That(JenkinsVolunteer.GetOwner(description), Is.Null);
      Assert.Throws<InvalidOperationException>(() => JenkinsVolunteer.SetOwner(description, null));
      Assert.Throws<InvalidOperationException>(() => JenkinsVolunteer.SetOwner(description, "other"));
    }

    [TestCase("")]
    [TestCase(" \t ")]
    [TestCase("alex\r\nother")]
    [TestCase("alex\0other")]
    [TestCase("alex\tother")]
    public void InvalidNamesAreRejectedBeforeWriting(string owner)
    {
      Assert.Throws<ArgumentException>(() => JenkinsVolunteer.SetOwner("Notes", owner));
    }

    [Test]
    public void InvalidUnicodeIsRejectedRatherThanSilentlyChangingTheOwner()
    {
      var owner = new string((char)0xD800, 1);
      Assert.Throws<System.Text.EncoderFallbackException>(() => JenkinsVolunteer.SetOwner("Notes", owner));
    }

    [TestCase("No notice, just Being fixed by alex")]
    [TestCase("<!-- unrelated comment -->\r\n<p>Release notes &amp; links</p>")]
    [TestCase("<!-- whatson:volunteer:start -->Other application's text<!-- whatson:volunteer:end -->")]
    public void OrdinaryDescriptionsAreNeverMistakenForNotices(string description)
    {
      Assert.That(JenkinsVolunteer.GetOwner(description), Is.Null);
      Assert.That(JenkinsVolunteer.SetOwner(description, null), Is.EqualTo(description));
      Assert.That(JenkinsVolunteer.SetOwner(JenkinsVolunteer.SetOwner(description, "alex"), null), Is.EqualTo(description));
    }

    [Test]
    public void EscapedNoticeExamplesRemainOrdinaryDescriptionText()
    {
      var description = "<pre>" + WebUtility.HtmlEncode(JenkinsVolunteer.SetOwner(null, "alex")) + "</pre>";
      Assert.That(JenkinsVolunteer.GetOwner(description), Is.Null);
      Assert.That(JenkinsVolunteer.SetOwner(description, null), Is.EqualTo(description));
    }

    [Test]
    public void ManyUnclosedMarkersDoNotBreakStatusReading()
    {
      var description = string.Concat(Enumerable.Repeat(Start + Owner + "Notice\n", 10000));
      Assert.That(JenkinsVolunteer.GetOwner(description), Is.Null);
      Assert.Throws<InvalidOperationException>(() => JenkinsVolunteer.SetOwner(description, "alex"));
    }

    [Test]
    public void LocationWhitespaceAndSpecialNamesRoundTripWithoutChangingSurroundingText()
    {
      var contexts = new[] { string.Empty, "\r\n", "<p>Notes</p>", "$1 ${owner} \\ path", "中文 &amp; 😀", new string('x', 65536) };
      var owners = new[] { "alex", "Jörg 中文 😀", "'\"<&> $1 ${owner}", "--><!-- whatsON:volunteer:end -->" };
      var whitespace = new[] { string.Empty, "\n", "\r\n", "\n\n", "\t \r\n" };
      foreach (var before in contexts)
      {
        foreach (var after in contexts)
        {
          foreach (var owner in owners)
          {
            foreach (var gap in whitespace)
            {
              var marker = JenkinsVolunteer.SetOwner(null, owner).Replace("\n", gap);
              var description = before + marker + after;
              Assert.That(JenkinsVolunteer.GetOwner(description), Is.EqualTo(owner));
              Assert.That(JenkinsVolunteer.SetOwner(description, null), Is.EqualTo(before + after));
              var replaced = JenkinsVolunteer.SetOwner(description, "replacement");
              Assert.That(replaced, Is.EqualTo(before + JenkinsVolunteer.SetOwner(null, "replacement") + after));
              Assert.That(JenkinsVolunteer.SetOwner(replaced, "replacement"), Is.EqualTo(replaced));
            }
          }
        }
      }
    }

    private static IEnumerable<string> InvalidNotices()
    {
      var valid = Start + Owner + "Notice" + End;
      yield return valid + valid;
      yield return Start + Owner + valid + End;
      yield return End + valid;
      yield return valid + Start;
      yield return Start + Owner;
      yield return Owner + End;
      yield return Owner;
      yield return Start + Owner + Owner + End;
      yield return valid + "\n[whatsON] Being fixed by other [/whatsON]\n";
      foreach (var encodedOwner in new[] { "A", "/w==", "IA==", "Cg==", "AA==", string.Empty, "%%%" })
      {
        yield return Start + "<!-- whatsON:volunteer:owner " + encodedOwner + " -->Notice" + End;
      }
    }
  }
}
