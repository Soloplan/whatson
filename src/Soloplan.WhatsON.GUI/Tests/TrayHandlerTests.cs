// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrayHandlerTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System.Collections.Generic;
  using System.Reflection;
  using System.Threading;
  using System.Windows.Forms;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [SetUICulture("en-US")]
  public class TrayHandlerTests
  {
    private const ObservationState Success = ObservationState.Success;

    private const ObservationState Failure = ObservationState.Failure;

    private const ObservationState Running = ObservationState.Running;

    /// <summary>
    /// Tests the <see cref="TrayHandler.GetOverallState" /> method.
    /// </summary>
    /// <param name="states">The states.</param>
    /// <param name="configuredConnectorCount">The configured connector count.</param>
    /// <returns>A <see cref="ObservationState" />.</returns>
    [TestCase(new ObservationState[0], 0, ExpectedResult = ObservationState.Unknown)]
    [TestCase(new ObservationState[0], 1, ExpectedResult = ObservationState.Unknown)]
    [TestCase(new[] { ObservationState.Success }, 1, ExpectedResult = ObservationState.Success)]
    [TestCase(new[] { ObservationState.Success }, 2, ExpectedResult = ObservationState.Unknown)]
    [TestCase(new[] { ObservationState.Success, ObservationState.Running }, 2, ExpectedResult = ObservationState.Unknown)]
    [TestCase(new[] { ObservationState.Running }, 1, ExpectedResult = ObservationState.Unknown)]
    [TestCase(new[] { ObservationState.Failure, ObservationState.Running }, 2, ExpectedResult = ObservationState.Failure)]
    [TestCase(new[] { ObservationState.Unstable, ObservationState.Running }, 2, ExpectedResult = ObservationState.Unstable)]
    [TestCase(new[] { ObservationState.Unknown, ObservationState.Running }, 3, ExpectedResult = ObservationState.Unknown)]
    [TestCase(new[] { ObservationState.Unknown, ObservationState.Success }, 2, ExpectedResult = ObservationState.Unknown)]
    [TestCase(new[] { ObservationState.Success, ObservationState.Unstable }, 3, ExpectedResult = ObservationState.Unstable)]
    [TestCase(new[] { ObservationState.Unstable, ObservationState.Failure }, 3, ExpectedResult = ObservationState.Failure)]
    [TestCase(new[] { ObservationState.Failure, ObservationState.Unstable, ObservationState.Success }, 3, ExpectedResult = ObservationState.Failure)]
    public ObservationState GetOverallStateTest(ObservationState[] states, int configuredConnectorCount) => TrayHandler.GetOverallState(states, configuredConnectorCount);

    [Test]
    [Apartment(ApartmentState.STA)]
    public void EmptyConfigurationHasANeutralIconAndAnExplicitTooltip()
    {
      var configuration = new ApplicationConfiguration { OpenMinimized = true };
      using (var tray = new TrayHandler(new ObservationScheduler(), configuration))
      {
        var icon = (typeof(TrayHandler).GetField("icon", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected member was not found.")).GetValue(tray) as NotifyIcon ?? throw new AssertionException("Expected icon.");
        var neutralIcon = (typeof(TrayHandler).GetField("loadingIcon", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected member was not found.")).GetValue(tray);
        var update = typeof(TrayHandler).GetMethod("UpdateTrayIcon", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected member was not found.");
        Assert.That(icon.Icon, Is.SameAs(neutralIcon), "An empty configuration must not show a green success badge.");
        Assert.That(icon.Text, Is.EqualTo("WhatsON - No projects configured"));

        configuration.ConnectorsConfiguration.Add(new ConnectorConfiguration("Jenkins", "Unloaded project", new List<ConfigurationItem>()));
        update.Invoke(tray, null);
        Assert.That(icon.Icon, Is.SameAs(neutralIcon));
        Assert.That(icon.Text, Is.EqualTo("WhatsON - Checking build status"));

        configuration.ConnectorsConfiguration.Clear();
        update.Invoke(tray, null);
        Assert.That(icon.Icon, Is.SameAs(neutralIcon));
        Assert.That(icon.Text, Is.EqualTo("WhatsON - No projects configured"));
      }
    }

    [TestCase(ObservationState.Success)]
    [TestCase(ObservationState.Failure)]
    [TestCase(ObservationState.Unstable)]
    [TestCase(ObservationState.Unknown)]
    [Apartment(ApartmentState.STA)]
    public void TrayIconFollowsRepeatedBuildStartsAndCompletions(ObservationState previousState)
    {
      using var tray = new TrayHandler(new ObservationScheduler(), new ApplicationConfiguration { OpenMinimized = true });
      var model = (typeof(TrayHandler).GetField("model", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected model field.")).GetValue(tray) as NotificationsModel ?? throw new AssertionException("Expected model.");
      var icon = (typeof(TrayHandler).GetField("icon", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected icon field.")).GetValue(tray) as NotifyIcon ?? throw new AssertionException("Expected icon.");
      var update = typeof(TrayHandler).GetMethod("UpdateTrayIcon", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected update method.");
      var connector = new ConnectorViewModel();
      model.Connectors.Add(connector);
      var history = new ConnectorViewModel();
      history.CurrentStatus.Update(new Status(previousState));
      connector.ConnectorSnapshots.Add(history.CurrentStatus);
      var runningIcons = (typeof(TrayHandler).GetField("runningIcons", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected running icons field.")).GetValue(tray) as Dictionary<ObservationState, System.Drawing.Icon[]> ?? throw new AssertionException("Expected running icons.");
      var pulse = typeof(TrayHandler).GetMethod("PulseTrayIcon", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected pulse method.");
      var timer = (typeof(TrayHandler).GetField("pulseTimer", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected timer field.")).GetValue(tray) as System.Windows.Forms.Timer ?? throw new AssertionException("Expected timer.");
      Assert.That(timer.Interval, Is.EqualTo(150));
      foreach (var state in new[] { previousState, Running, Failure, Running, Success })
      {
        connector.CurrentStatus.Update(new Status(state));
        update.Invoke(tray, null);
        var effectiveState = state == Running ? previousState : state;
        var expectedField = effectiveState switch
        {
          ObservationState.Failure => "failureIcon",
          ObservationState.Unstable => "unstableIcon",
          ObservationState.Unknown => "loadingIcon",
          _ => "normalIcon",
        };
        var expectedIcon = state == Running ? runningIcons[effectiveState][0] : (typeof(TrayHandler).GetField(expectedField, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected status icon field.")).GetValue(tray);
        Assert.That(icon.Icon, Is.SameAs(expectedIcon), $"Tray icon after changing to {state}.");
        Assert.That(timer.Enabled, Is.EqualTo(state == Running));
        if (state == Running)
        {
          var frames = runningIcons[effectiveState];
          using var expanded = frames[0].ToBitmap();
          using var contracted = frames[frames.Length / 2].ToBitmap();
          var steadyIcon = (typeof(TrayHandler).GetField(expectedField, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected status icon field.")).GetValue(tray) as System.Drawing.Icon ?? throw new AssertionException("Expected status icon.");
          using var steady = steadyIcon.ToBitmap();
          var color = effectiveState switch
          {
            ObservationState.Success => System.Drawing.Color.LimeGreen,
            ObservationState.Failure => System.Drawing.Color.Red,
            ObservationState.Unstable => System.Drawing.Color.Gold,
            _ => System.Drawing.Color.Gray,
          };
          var expandedPixels = 0;
          var contractedPixels = 0;
          var steadyPixels = 0;
          for (var y = 0; y < expanded.Height; y++)
          {
            for (var x = 0; x < expanded.Width; x++)
            {
              expandedPixels += expanded.GetPixel(x, y).ToArgb() == color.ToArgb() ? 1 : 0;
              contractedPixels += contracted.GetPixel(x, y).ToArgb() == color.ToArgb() ? 1 : 0;
              steadyPixels += steady.GetPixel(x, y).ToArgb() == color.ToArgb() ? 1 : 0;
            }
          }

          Assert.That(expandedPixels - contractedPixels, Is.GreaterThan(20), "The pulse must visibly change the badge area.");
          if (effectiveState != ObservationState.Unknown)
          {
            Assert.That(expandedPixels, Is.EqualTo(steadyPixels), "The pulse must not grow beyond the normal badge size.");
          }

          Assert.That(contracted.GetPixel(11, 11).ToArgb(), Is.EqualTo(color.ToArgb()), "The contracted badge must keep the same center and status color.");
          Assert.That(contracted.GetPixel(13, 13).ToArgb(), Is.Not.EqualTo(color.ToArgb()), "The contracted badge must not shift toward the corner.");
          for (var frame = 1; frame <= frames.Length; frame++)
          {
            pulse.Invoke(tray, new object?[] { null, System.EventArgs.Empty });
            Assert.That(icon.Icon, Is.SameAs(frames[frame % frames.Length]));
          }

          Assert.That(icon.Text, Does.EndWith($" ({Soloplan.WhatsON.Localization.Strings.Running})"));
          if (previousState == Failure)
          {
            Assert.That(icon.Text, Does.StartWith(Properties.Resources.TrayHandler_UpdateTrayIcon_BuildFailing));
          }
        }
        else
        {
          pulse.Invoke(tray, new object?[] { null, System.EventArgs.Empty });
          Assert.That(icon.Icon, Is.SameAs(expectedIcon), "Completed builds must stay steady.");
        }
      }
    }

    /// <summary>
    /// Tests the <see cref="TrayHandler.GetEffectiveState" /> method.
    /// </summary>
    /// <param name="currentState">The current state.</param>
    /// <param name="snapshotStates">The snapshot states.</param>
    /// <returns>A <see cref="ObservationState" />.</returns>
    [TestCase(ObservationState.Success, new[] { ObservationState.Failure }, ExpectedResult = ObservationState.Success)]
    [TestCase(ObservationState.Failure, new[] { ObservationState.Success }, ExpectedResult = ObservationState.Failure)]
    [TestCase(ObservationState.Running, new[] { ObservationState.Unstable, ObservationState.Success }, ExpectedResult = ObservationState.Unstable)]
    [TestCase(ObservationState.Running, new[] { ObservationState.Failure }, ExpectedResult = ObservationState.Failure)]
    [TestCase(ObservationState.Running, new[] { ObservationState.Success }, ExpectedResult = ObservationState.Success)]
    [TestCase(
      ObservationState.Unknown,
      new[] { ObservationState.Running, ObservationState.Failure, ObservationState.Success },
      ExpectedResult = ObservationState.Failure)]
    [TestCase(ObservationState.Unknown, new[] { ObservationState.Unknown, ObservationState.Running }, ExpectedResult = ObservationState.Unknown)]
    public ObservationState GetEffectiveStateTest(ObservationState currentState, ObservationState[] snapshotStates) => TrayHandler.GetEffectiveState(currentState, snapshotStates);

    // usually the status queue contains 5 elements but the newest one is the same current, so it is enough to give 4 states. The newest is on the left
    [TestCase(false, Success, Success, Success, Success, Success, ExpectedResult = true, Description = "Simple state update")]
    [TestCase(false, Success, Failure, Failure, Failure, Failure, ExpectedResult = true, Description = "Simple state change")]
    [TestCase(
      true,
      Success,
      Success,
      Success,
      Success,
      Success,
      ExpectedResult = false,
      Description = "Do not show notification if show only on change and the change was not present")]
    [TestCase(
      true,
      Success,
      Failure,
      Success,
      Success,
      Success,
      ExpectedResult = true,
      Description = "Show notification when the change was present and it's configured to show it only on change")]
    [TestCase(
      true,
      Running,
      Success,
      Success,
      Success,
      Success,
      ExpectedResult = false,
      Description = "Do not show notification if we change to inactive state")]
    [TestCase(
      false,
      Running,
      Success,
      Success,
      Success,
      Success,
      ExpectedResult = false,
      Description = "Do not show notification if we change to inactive state")]
    public bool CheckNotificationShowTest(
      bool onlyIfChanged,
      ObservationState currentState,
      ObservationState historyState1,
      ObservationState historyState2,
      ObservationState historyState3,
      ObservationState historyState4)
    {
      var settings = new NotificationConfiguration { OnlyIfChanged = onlyIfChanged, RunningNotificationEnabled = false };
      var current = new Status(currentState) { BuildNumber = 5 };
      var history = new[]
      {
        current,
        new Status(historyState1) { BuildNumber = 4 },
        new Status(historyState2) { BuildNumber = 3 },
        new Status(historyState3) { BuildNumber = 2 },
        new Status(historyState4) { BuildNumber = 1 },
      };
      return settings.ShouldNotify(current, history);
    }
  }
}
