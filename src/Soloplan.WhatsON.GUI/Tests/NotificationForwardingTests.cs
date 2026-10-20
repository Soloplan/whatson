// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationForwardingTests.cs" company="Soloplan GmbH">
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
  using System.Reflection;
  using System.Threading;
  using System.Threading.Tasks;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Exercises event forwarding without an Application, dispatcher, window, or tray icon.
  /// </summary>
  [TestFixture]
  public class NotificationForwardingTests
  {
    [Test]
    public void EveryStateTransitionAndNewBuildIsForwardedExactlyOnce()
    {
      foreach (var before in Enum.GetValues<ObservationState>())
      {
        foreach (var after in Enum.GetValues<ObservationState>())
        {
          var connector = new TestConnector();
          using var model = new NotificationsModel(new ObservationScheduler());
          var viewModel = new TestViewModel(connector);
          model.Connectors.Add(viewModel);
          var states = 0;
          model.StatusChanged += (sender, args) =>
          {
            if (args.PropertyName == nameof(StatusViewModel.State))
            {
              Assert.That(sender, Is.SameAs(viewModel.CurrentStatus));
              states++;
            }
          };

          connector.CurrentStatus = new Status(before) { BuildNumber = 1 };
          Query(model, connector);
          Assert.That(states, Is.EqualTo(1), $"First observation: {before}");
          Query(model, connector);
          Assert.That(states, Is.EqualTo(1), "Repeated polls must not notify again.");

          connector.CurrentStatus = new Status(after) { BuildNumber = 1 };
          Query(model, connector);
          var expected = before == after ? 1 : 2;
          Assert.That(states, Is.EqualTo(expected), $"{before} -> {after}");

          connector.CurrentStatus = new Status(after) { BuildNumber = 2 };
          Query(model, connector);
          Assert.That(states, Is.EqualTo(expected + 1), "A new build with the same result must reach the notification policy.");
          Query(model, connector);
          Assert.That(states, Is.EqualTo(expected + 1));
        }
      }
    }

    [Test]
    public void DisposalRemovesSchedulerAndViewModelSubscriptions()
    {
      var scheduler = new ObservationScheduler();
      var connector = new TestConnector { CurrentStatus = new Status(ObservationState.Success) };
      var model = new NotificationsModel(scheduler);
      var viewModel = new TestViewModel(connector);
      model.Connectors.Add(viewModel);
      Query(model, connector);
      model.Dispose();
      model.Dispose();

      var changes = new List<PropertyChangedEventArgs>();
      model.StatusChanged += (sender, args) => changes.Add(args);
      viewModel.Name = "Changed";
      viewModel.CurrentStatus.State = ObservationState.Failure;
      Assert.That(changes, Is.Empty);
      var queried = typeof(ObservationScheduler).GetField("StatusQueried", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new AssertionException("Expected scheduler event.");
      var handlers = queried.GetValue(scheduler) as Delegate;
      Assert.That(handlers?.GetInvocationList().Any(handler => ReferenceEquals(handler.Target, model)) ?? false, Is.False);
    }

    private static void Query(NotificationsModel model, Connector connector)
    {
      var query = typeof(NotificationsModel).GetMethod("SchedulerStatusQueried", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new AssertionException("Expected query handler.");
      query.Invoke(model, new object?[] { null, connector });
    }

    private sealed class TestViewModel : ConnectorViewModel
    {
      public TestViewModel(Connector connector)
        : base(connector)
      {
      }

      public override void Update(Connector? changedConnector) => this.CurrentStatus.Update(changedConnector?.CurrentStatus);
    }

    private sealed class TestConnector : Connector
    {
      public TestConnector()
        : base(new ConnectorConfiguration("Fake", "Project"))
      {
      }

      protected override Task<Status?> GetCurrentStatus(CancellationToken cancellationToken) => Task.FromResult<Status?>(null);

      protected override Task<List<Status?>> GetHistory(CancellationToken cancellationToken) => Task.FromResult(new List<Status?>());
    }
  }
}
