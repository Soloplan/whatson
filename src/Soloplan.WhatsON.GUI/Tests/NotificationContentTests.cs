// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationContentTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using NUnit.Framework;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Jenkins.GUI;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [SetUICulture("en-US")]
  public class NotificationContentTests
  {
    [TestCase(1144, "19 min 04 s")]
    [TestCase(3660, "1 h 01 min")]
    [TestCase(176400, "49 h 00 min")]
    public void CompactDurationIncludesUnitsAndDoesNotWrapAtOneDay(int seconds, string expected)
    {
      var status = new BuildStatusViewModel(new ConnectorViewModel());
      status.Update(new Status(ObservationState.Success) { Duration = TimeSpan.FromSeconds(seconds) });
      Assert.That(status.TooltipDuration, Is.EqualTo(expected));
    }

    [TestCase(ObservationState.Failure, true)]
    [TestCase(ObservationState.Unstable, true)]
    [TestCase(ObservationState.Success, false)]
    [TestCase(ObservationState.Running, false)]
    [TestCase(ObservationState.Unknown, false)]
    public void FailureSummaryIsSharedByNotificationsAndTooltipsOnlyForBrokenBuilds(ObservationState state, bool show)
    {
      var project = new ConnectorViewModel { Name = "Release" };
      project.CurrentStatus.Update(new Status(state) { FailedTestCount = 2, FailureDetails = "Compiler error\nTests.First\nTests.Second" });
      var summary = project.CurrentStatus.FailureSummary;
      if (show)
      {
        Assert.That(summary, Is.EqualTo($"Failed tests: 2{Environment.NewLine}Compiler error{Environment.NewLine}Tests.First{Environment.NewLine}Tests.Second"));
        Assert.That(TrayHandler.GetNotificationDescription(project.CurrentStatus), Does.EndWith(summary ?? throw new AssertionException("Missing failure summary.")));
      }
      else
      {
        Assert.That(summary, Is.Null);
        Assert.That(TrayHandler.GetNotificationDescription(project.CurrentStatus), Does.Not.Contain("Tests"));
      }
    }

    [Test]
    [SetUICulture("de-DE")]
    public void FailureCountIsLocalizedAndDetailsAreBounded()
    {
      var status = new StatusViewModel(new ConnectorViewModel());
      status.Update(new Status(ObservationState.Failure) { FailedTestCount = 20, FailureDetails = new string('x', 1000) });
      Assert.That(status.FailureSummary, Does.StartWith("Fehlgeschlagene Tests: 20"));
      Assert.That(status.FailureSummary, Does.EndWith("..."));
      Assert.That(status.FailureSummary?.Length, Is.LessThan(200));
    }

    [Test]
    public void LateReportsRefreshBindingsAndLoadingClearsOldFailures()
    {
      var status = new StatusViewModel(new ConnectorViewModel());
      var changes = new List<string?>();
      ((INotifyPropertyChanged)status).PropertyChanged += (_, args) => changes.Add(args.PropertyName);
      status.Update(new Status(ObservationState.Failure) { FailedTestCount = 1 });
      changes.Clear();
      status.Update(new Status(ObservationState.Failure) { FailedTestCount = 1, FailureDetails = "Tests.First" });
      Assert.That(changes, Does.Contain(nameof(status.FailureSummary)));
      Assert.That(changes, Does.Contain(nameof(status.FailurePreviewGroups)));
      Assert.That(changes, Does.Contain(nameof(status.HasFailurePreview)));
      Assert.That(status.FailureSummary, Does.Contain("Tests.First"));
      status.Update(null);
      Assert.That(status.FailureSummary, Is.Null);
      Assert.That(status.HasFailurePreview, Is.False);
    }

    [Test]
    public void CompactPreviewGroupsTwoTestsAndKeepsWarningsAtTheBottom()
    {
      var status = new JenkinsStatusViewModel(new ConnectorViewModel());
      status.Update(new JenkinsStatus(ObservationState.Unstable)
      {
        FailedTestCount = 4,
        SkippedTestCount = 1,
        FailureDetails = "Example.Product.Tests.LayoutTests.FirstAsync\nExample.Product.Tests.LayoutTests.SecondAsync\nExample.Product.Tests.OtherTests.ThirdAsync\nExample.Product.Tests.OtherTests.FourthAsync\nSKIPPED: Example.Product.Tests.OtherTests.FifthAsync",
      });

      Assert.That(status.FailurePreviewGroups.Select(group => group.Key), Is.EqualTo(new[] { "LayoutTests" }));
      Assert.That(status.FailurePreviewGroups.Single(), Is.EqualTo(new[] { "FirstAsync", "SecondAsync" }));
      Assert.That(status.FailurePreviewHeading, Is.EqualTo("4 failed tests"));
      Assert.That(status.MoreFailuresPreview, Is.EqualTo("2 more failures in the test report"));
      Assert.That(status.AdditionalFailureGroups.Single().Key, Is.EqualTo("OtherTests"));
      Assert.That(status.AdditionalFailureGroups.Single(), Is.EqualTo(new[] { "ThirdAsync", "FourthAsync" }));
      Assert.That(status.WarningPreview, Is.EqualTo(Strings.SkippedTestsNotice));
      Assert.That(status.FailureSummary, Does.Contain("Example.Product.Tests.OtherTests.FourthAsync"), "Notifications retain their original details.");
    }

    [Test]
    public void CompilerPreviewKeepsSourceAndDiagnosticCode()
    {
      var status = new StatusViewModel(new ConnectorViewModel());
      status.Update(new Status(ObservationState.Failure)
      {
        FailureDetails = "ExampleView.cs(42,7): CS1061: Missing member missingMember\nWARNING: Some tests were not run",
      });

      Assert.That(status.FailurePreviewGroups.Single().Key, Is.EqualTo("ExampleView.cs(42,7) · CS1061"));
      Assert.That(status.FailurePreviewGroups.Single().Single(), Is.EqualTo("Missing member missingMember"));
      Assert.That(status.WarningPreview, Does.StartWith("WARNING:"));
      Assert.That(status.MoreFailuresPreview, Is.Null);
    }

    [Test]
    public void LongTestNamesWrapBetweenPhrasesWithoutChangingNotificationText()
    {
      const string Method = "ReadTestStructureReturnsSampleStructureOfAllTestDataItemsAsync";
      var status = new StatusViewModel(new ConnectorViewModel());
      status.Update(new Status(ObservationState.Unstable) { FailureDetails = "Tests.LayoutTests." + Method });
      Assert.That(status.FailurePreviewGroups.Single().Single(), Is.EqualTo("ReadTestStructure\u200BReturnsSampleStructure\u200BOfAllTestDataItemsAsync"));
      Assert.That(status.FailureSummary, Does.EndWith(Method));
    }

    [TestCase(ObservationState.Success)]
    [TestCase(ObservationState.Failure)]
    [TestCase(ObservationState.Unstable)]
    [TestCase(ObservationState.Running)]
    public void FullBuildLabelAndProjectAreIncluded(ObservationState state)
    {
      const string DisplayName = "Release candidate for the desktop application with all connector plugins and packaging checks #42";
      var project = new ConnectorViewModel { Name = "Products / Desktop / Release" };
      var status = new JenkinsStatusViewModel(project);
      status.Update(new JenkinsStatus(state) { BuildNumber = 42, DisplayName = DisplayName, Time = DateTime.UtcNow });

      Assert.That(TrayHandler.GetNotificationDescription(status), Is.EqualTo($"{Localizer.ProjectNotification(project.Name)}{Environment.NewLine}{DisplayName}"));
    }

    [TestCase(null, "#42")]
    [TestCase("", "#42")]
    [TestCase("Release candidate", "Release candidate (#42)")]
    [TestCase("Release #42", "Release #42")]
    public void BuildNumberIsAvailableWithoutDuplicatingIt(string? displayName, string expectedLabel)
    {
      var project = new ConnectorViewModel { Name = "Release" };
      var status = new JenkinsStatusViewModel(project);
      status.Update(new JenkinsStatus(ObservationState.Success) { BuildNumber = 42, DisplayName = displayName });

      Assert.That(TrayHandler.GetNotificationDescription(status), Is.EqualTo($"{Localizer.ProjectNotification(project.Name)}{Environment.NewLine}{expectedLabel}"));
    }

    [Test]
    [SetUICulture("de-DE")]
    public void CompletedBuildIncludesCustomLabelAndLocalizedDuration()
    {
      var project = new ConnectorViewModel { Name = "Release" };
      project.CurrentStatus.Update(new Status(ObservationState.Failure) { Label = "release-2026.10", Duration = TimeSpan.FromSeconds(125) });

      Assert.That(TrayHandler.GetNotificationDescription(project.CurrentStatus), Is.EqualTo($"{Localizer.ProjectNotification(project.Name)}{Environment.NewLine}release-2026.10{Environment.NewLine}{Strings.DurationPrefix}{TimeSpan.FromSeconds(125):g}"));
    }

    [Test]
    public void RunningBuildOmitsCompletedDuration()
    {
      var project = new ConnectorViewModel { Name = "Release" };
      project.CurrentStatus.Update(new Status(ObservationState.Running) { BuildNumber = 42, Duration = TimeSpan.FromSeconds(125) });

      Assert.That(TrayHandler.GetNotificationDescription(project.CurrentStatus), Is.EqualTo($"{Localizer.ProjectNotification(project.Name)}{Environment.NewLine}#42"));
    }

    [Test]
    public void MissingBuildDetailsLeaveTheProjectMessage()
    {
      var project = new ConnectorViewModel { Name = "Release" };
      var status = new StatusViewModel(project);

      Assert.That(TrayHandler.GetNotificationDescription(status), Is.EqualTo(Localizer.ProjectNotification(project.Name)));
    }
  }
}
