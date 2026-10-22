// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationDecisionTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System.Linq;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class NotificationDecisionTests
  {
    [TestCase(ObservationState.Success)]
    [TestCase(ObservationState.Failure)]
    [TestCase(ObservationState.Unstable)]
    [TestCase(ObservationState.Unknown)]
    [TestCase(ObservationState.Running)]
    public void DisabledStatesNeverShowNotifications(ObservationState state)
    {
      var settings = new NotificationConfiguration();
      settings.AssignFromObeservationStateActivity(state, false);

      Assert.That(ShouldNotify(state, state, settings, state, ObservationState.Success), Is.False);
    }

    [TestCase(ObservationState.Success, true)]
    [TestCase(ObservationState.Failure, false)]
    public void ChangeDetectionSkipsDisabledIntermediateStates(ObservationState lastCompletedState, bool expected)
    {
      var settings = new NotificationConfiguration { OnlyIfChanged = true, RunningNotificationEnabled = false, UnknownNotificationEnabled = false };

      var history = new[] { ObservationState.Failure, ObservationState.Running, ObservationState.Unknown, lastCompletedState };

      Assert.That(ShouldNotify(ObservationState.Failure, ObservationState.Failure, settings, history), Is.EqualTo(expected));
    }

    [TestCase(ObservationState.Success, ObservationState.Failure, false)]
    [TestCase(ObservationState.Failure, ObservationState.Success, true)]
    public void OnlyTheMostRecentEnabledHistoryStateDeterminesWhetherTheStateChanged(ObservationState previous, ObservationState older, bool expected)
    {
      var settings = new NotificationConfiguration { OnlyIfChanged = true };

      Assert.That(
        ShouldNotify(ObservationState.Success, ObservationState.Success, settings, ObservationState.Success, previous, older),
        Is.EqualTo(expected));
    }

    [Test]
    public void FirstCompletedBuildCanNotifyEvenWhenOnlyChangesAreEnabled()
    {
      var settings = new NotificationConfiguration { OnlyIfChanged = true };

      Assert.That(ShouldNotify(ObservationState.Failure, ObservationState.Failure, settings, ObservationState.Failure), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ANotificationForAnotherStateIsNeverShown(bool onlyIfChanged)
    {
      var settings = new NotificationConfiguration { OnlyIfChanged = onlyIfChanged };

      Assert.That(
        ShouldNotify(ObservationState.Success, ObservationState.Failure, settings, ObservationState.Success, ObservationState.Failure),
        Is.False);
    }

    [Test]
    public void RunningNotificationsCanBeExplicitlyEnabled()
    {
      var settings = new NotificationConfiguration { RunningNotificationEnabled = true };

      Assert.That(
        ShouldNotify(ObservationState.Running, ObservationState.Running, settings, ObservationState.Running, ObservationState.Success),
        Is.True);
    }

    [TestCase(ObservationState.Running)]
    [TestCase(ObservationState.Unknown)]
    public void AProjectWithoutCompletedHistoryKeepsItsActualState(ObservationState current)
    {
      Assert.That(TrayHandler.GetEffectiveState(current, new ObservationState[0]), Is.EqualTo(current));
      Assert.That(TrayHandler.GetEffectiveState(current, [ObservationState.Running, ObservationState.Unknown]), Is.EqualTo(current));
    }

    [Test]
    public void AFailingProjectRemainsVisibleWhileOtherProjectsAreStillLoading() => Assert.That(TrayHandler.GetOverallState([ObservationState.Unknown, ObservationState.Failure], 5), Is.EqualTo(ObservationState.Failure));

    [Test]
    public void AnUnstableProjectRemainsVisibleWhileOtherProjectsAreStillLoading()
    {
      Assert.That(
        TrayHandler.GetOverallState([ObservationState.Unstable, ObservationState.Unknown], 5),
        Is.EqualTo(ObservationState.Unstable));
    }

    private static bool ShouldNotify(
      ObservationState current,
      ObservationState notificationState,
      NotificationConfiguration settings,
      params ObservationState[] newestFirst)
    {
      var history = newestFirst.Select((state, index) => new Status(state) { BuildNumber = 100 - index }).ToArray();
      var status = new Status(current) { BuildNumber = 100 };
      return current == notificationState && settings.ShouldNotify(status, history);
    }
  }
}
