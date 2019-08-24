// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrayHandlerTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Serialization;

  [TestFixture]
  public class TrayHandlerTests
  {
    private const ObservationState Success = ObservationState.Success;
    private const ObservationState Failure = ObservationState.Failure;
    private const ObservationState Running = ObservationState.Running;

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
      var status1 = new StatusViewModel(connectorViewModel) { State = currentState };
      var status2 = new StatusViewModel(connectorViewModel) { State = historyState1 };
      var status3 = new StatusViewModel(connectorViewModel) { State = historyState2 };
      var status4 = new StatusViewModel(connectorViewModel) { State = historyState3 };
      var status5 = new StatusViewModel(connectorViewModel) { State = historyState4 };
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