// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationContentTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using NUnit.Framework;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Jenkins.GUI;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [SetUICulture("en-US")]
  public class NotificationContentTests
  {
    [TestCase(ObservationState.Success)]
    [TestCase(ObservationState.Failure)]
    [TestCase(ObservationState.Unstable)]
    [TestCase(ObservationState.Running)]
    public void FullBuildLabelAndProjectAreIncluded(ObservationState state)
    {
      const string displayName = "Release candidate for the desktop application with all connector plugins and packaging checks #42";
      var project = new ConnectorViewModel { Name = "Products / Desktop / Release" };
      var status = new JenkinsStatusViewModel(project);
      status.Update(new JenkinsStatus(state) { BuildNumber = 42, DisplayName = displayName, Time = DateTime.UtcNow });

      Assert.That(TrayHandler.GetNotificationDescription(status), Is.EqualTo($"{Localizer.ProjectNotification(project.Name)}{Environment.NewLine}{displayName}"));
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
