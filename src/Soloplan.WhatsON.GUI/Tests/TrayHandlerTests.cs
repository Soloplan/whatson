// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrayHandlerTests.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  public class TrayHandlerTests
  {
    private const ObservationState Success = ObservationState.Success;
    private const ObservationState Failure = ObservationState.Failure;
    private const ObservationState Running = ObservationState.Running;

    /// <summary>
    /// Tests the <see cref="TrayHandler.GetOverallState"/> method.
    /// </summary>
    /// <param name="states">The states.</param>
    /// <param name="configuredConnectorCount">The configured connector count.</param>
    /// <returns>A <see cref="ObservationState"/>.</returns>
    [TestCase(new ObservationState[0], 0, ExpectedResult = ObservationState.Success)]
    [TestCase(new ObservationState[0], 1, ExpectedResult = ObservationState.Unknown)]
    [TestCase(new[] { ObservationState.Success }, 1, ExpectedResult = ObservationState.Success)]
    [TestCase(new[] { ObservationState.Success }, 2, ExpectedResult = ObservationState.Unknown)]
    [TestCase(new[] { ObservationState.Success, ObservationState.Running }, 2, ExpectedResult = ObservationState.Success)]
    [TestCase(new[] { ObservationState.Unknown, ObservationState.Success }, 2, ExpectedResult = ObservationState.Unknown)]
    [TestCase(new[] { ObservationState.Success, ObservationState.Unstable }, 3, ExpectedResult = ObservationState.Unstable)]
    [TestCase(new[] { ObservationState.Unstable, ObservationState.Failure }, 3, ExpectedResult = ObservationState.Failure)]
    [TestCase(new[] { ObservationState.Failure, ObservationState.Unstable, ObservationState.Success }, 3, ExpectedResult = ObservationState.Failure)]
    public ObservationState GetOverallStateTest(ObservationState[] states, int configuredConnectorCount)
    {
      return TrayHandler.GetOverallState(states, configuredConnectorCount);
    }

    /// <summary>
    /// Tests the <see cref="TrayHandler.GetEffectiveState"/> method.
    /// </summary>
    /// <param name="currentState">The current state.</param>
    /// <param name="snapshotStates">The snapshot states.</param>
    /// <returns>A <see cref="ObservationState"/>.</returns>
    [TestCase(ObservationState.Success, new[] { ObservationState.Failure }, ExpectedResult = ObservationState.Success)]
    [TestCase(ObservationState.Failure, new[] { ObservationState.Success }, ExpectedResult = ObservationState.Failure)]
    [TestCase(ObservationState.Running, new[] { ObservationState.Unstable, ObservationState.Success }, ExpectedResult = ObservationState.Unstable)]
    [TestCase(ObservationState.Unknown, new[] { ObservationState.Running, ObservationState.Failure, ObservationState.Success }, ExpectedResult = ObservationState.Failure)]
    [TestCase(ObservationState.Unknown, new[] { ObservationState.Unknown, ObservationState.Running }, ExpectedResult = ObservationState.Unknown)]
    public ObservationState GetEffectiveStateTest(ObservationState currentState, ObservationState[] snapshotStates)
    {
      return TrayHandler.GetEffectiveState(currentState, snapshotStates);
    }

    // usually the status queue contains 5 elements but the newest one is the same current, so it is enough to give 4 states. The newest is on the left
    [TestCase(false, Success, Success, Success, Success, Success, ExpectedResult = true, Description = "Simple state update")]
    [TestCase(false, Success, Failure, Failure, Failure, Failure, ExpectedResult = true, Description = "Simple state change")]
    [TestCase(true, Success, Success, Success, Success, Success, ExpectedResult = false, Description = "Do not show notification if show only on change and the change was not present")]
    [TestCase(true, Success, Failure, Success, Success, Success, ExpectedResult = true, Description = "Show notification when the change was present and it's configured to show it only on change")]
    [TestCase(true, Running, Success, Success, Success, Success, ExpectedResult = false, Description = "Do not show notification if we change to inactive state")]
    [TestCase(false, Running, Success, Success, Success, Success, ExpectedResult = false, Description = "Do not show notification if we change to inactive state")]
    public bool CheckNotificationShowTest(bool onlyIfChanged, ObservationState currentState, ObservationState historyState1, ObservationState historyState2, ObservationState historyState3, ObservationState historyState4)
    {
      var observationScheduler = new ObservationScheduler();
      var configuration = new ApplicationConfiguration();
      configuration.OpenMinimized = true;
      var trayHandler = new TrayHandler(observationScheduler, configuration);

      var connectorViewModel = new ConnectorViewModel();
      var statusViewModel = new StatusViewModel(connectorViewModel);
      statusViewModel.State = currentState;
      var status1 = new BuildStatusViewModel(connectorViewModel) { State = currentState };
      var status2 = new BuildStatusViewModel(connectorViewModel) { State = historyState1 };
      var status3 = new BuildStatusViewModel(connectorViewModel) { State = historyState2 };
      var status4 = new BuildStatusViewModel(connectorViewModel) { State = historyState3 };
      var status5 = new BuildStatusViewModel(connectorViewModel) { State = historyState4 };
      connectorViewModel.ConnectorSnapshots.Add(status5);
      connectorViewModel.ConnectorSnapshots.Add(status4);
      connectorViewModel.ConnectorSnapshots.Add(status3);
      connectorViewModel.ConnectorSnapshots.Add(status2);
      connectorViewModel.ConnectorSnapshots.Add(status1);

      var notificationConfiguration = new NotificationConfiguration();
      notificationConfiguration.OnlyIfChanged = onlyIfChanged;
      notificationConfiguration.RunningNotificationEnabled = false;

      return trayHandler.CheckNotificationShow(statusViewModel, currentState, notificationConfiguration);
    }
  }
}
