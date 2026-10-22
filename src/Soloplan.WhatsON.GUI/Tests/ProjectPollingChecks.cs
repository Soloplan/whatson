// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectPollingChecks.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Linq;
  using System.Reflection;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Threading;
  using NLog;
  using NLog.Config;
  using NLog.Targets;
  using NUnit.Framework;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  internal static class ProjectPollingChecks
  {
    internal static void Verify()
    {
      VerifyRemovedProjects();
      VerifyModelReinitialization();
      VerifyViewLifetime();
      VerifyGroupCountsAndMoves();
    }

    private static void VerifyGroupCountsAndMoves()
    {
      var scheduler = new ObservationScheduler();
      var connector = CreateConnector("Source");
      var other = CreateConnector("Source");
      var configuration = new ApplicationConfiguration();
      configuration.ConnectorsConfiguration.Add(connector.Configuration);
      configuration.ConnectorsConfiguration.Add(other.Configuration);
      using var model = new ConnectorTreeViewModel();
      model.Init(scheduler, configuration, new[] { connector, other });
      var source = model.ConnectorGroups.Single();
      var result = GetPendingResult(scheduler);
      foreach (var state in new[] { ObservationState.Failure, ObservationState.Running, ObservationState.Unstable, ObservationState.Success, ObservationState.Unknown })
      {
        connector.CurrentStatus = new Status(state);
        result(scheduler, connector);
        result(scheduler, connector);
        Assert.That(source.FailingConnectors, Is.EqualTo(state == ObservationState.Failure ? 1 : 0));
        Assert.That(source.RunningConnectors, Is.EqualTo(state == ObservationState.Running ? 1 : 0));
        Assert.That(source.UnstableConnectors, Is.EqualTo(state == ObservationState.Unstable ? 1 : 0));
        Assert.That(source.UnknownConnectors, Is.EqualTo(state == ObservationState.Unknown ? 2 : 1));
      }

      var row = source.ConnectorViewModels.First();
      var target = model.CreateGroup("Target");
      source.ConnectorViewModels.Remove(row);
      target.ConnectorViewModels.Add(row);
      connector.CurrentStatus = new Status(ObservationState.Failure);
      result(scheduler, connector);
      Assert.That(source.UnknownConnectors, Is.EqualTo(1));
      Assert.That(source.FailingConnectors, Is.Zero);
      Assert.That(target.FailingConnectors, Is.EqualTo(1));
      Assert.That(target.UnknownConnectors, Is.Zero);

      target.ConnectorViewModels.Clear();
      connector.CurrentStatus = new Status(ObservationState.Running);
      result(scheduler, connector);
      Assert.That(target.RunningConnectors, Is.Zero);
      Assert.That(row.CurrentStatus.State, Is.EqualTo(ObservationState.Failure));
      model.ConnectorGroups.Clear();
      result(scheduler, other);
      Assert.That(model.ConnectorGroups, Is.Empty);
    }

    private static void VerifyRemovedProjects()
    {
      var scheduler = new ObservationScheduler();
      var removed = CreateConnector("Removed");
      var remaining = CreateConnector("Remaining");
      var configuration = new ApplicationConfiguration();
      configuration.ConnectorsConfiguration.Add(removed.Configuration);
      configuration.ConnectorsConfiguration.Add(remaining.Configuration);
      using var model = new ConnectorTreeViewModel();
      model.Init(scheduler, configuration, new[] { removed, remaining });
      var pending = GetPendingResult(scheduler);
      var removedGroup = model.ConnectorGroups.First();
      var removedRow = removedGroup.ConnectorViewModels.Single();
      var remainingRow = model.ConnectorGroups.Last().ConnectorViewModels.Single();
      var previous = LogManager.Configuration;
      var target = new MemoryTarget("late-project-result") { Layout = "${level}|${message}" };
      var logging = new LoggingConfiguration();
      logging.AddRule(LogLevel.Trace, LogLevel.Fatal, target, typeof(ConnectorTreeViewModel).FullName);
      try
      {
        LogManager.Configuration = logging;
        removed.CurrentStatus = new Status { ErrorMessage = "Private build history" };
        CompleteAfterUiChange(
          () => pending(scheduler, removed),
          () => model.DeleteGroupAsync(null, new DeleteTreeItemEventArgs(removedGroup)));
        pending(scheduler, removed);
        Assert.That(removedRow.CurrentStatus.ErrorMessage, Is.EqualTo("Initial"));
        Assert.That(model.ConnectorGroups, Has.Count.EqualTo(1), "A late result must not restore a deleted group.");
        Assert.That(target.Logs, Is.Empty, "Expected late results must not log warnings or dump private connector data.");

        remaining.CurrentStatus = new Status { ErrorMessage = "Updated" };
        pending(scheduler, remaining);
        Assert.That(remainingRow.CurrentStatus.ErrorMessage, Is.EqualTo("Updated"), "Other projects must keep receiving updates.");

        model.DeleteGroupAsync(null, new DeleteTreeItemEventArgs(remainingRow));
        pending(scheduler, remaining);
        Assert.That(model.ConnectorGroups.SelectMany(group => group.ConnectorViewModels), Is.Empty);
        Assert.That(target.Logs, Has.None.Contains("Warn"));
        Assert.That(target.Logs, Has.None.Contains("Private build history"));
      }
      finally
      {
        LogManager.Configuration = previous;
      }
    }

    private static void VerifyModelReinitialization()
    {
      var firstScheduler = new ObservationScheduler();
      var secondScheduler = new ObservationScheduler();
      var connector = CreateConnector("Main");
      var configuration = new ApplicationConfiguration();
      configuration.ConnectorsConfiguration.Add(connector.Configuration);
      using var model = new ConnectorTreeViewModel();
      model.Init(firstScheduler, configuration, new[] { connector });
      var oldResult = GetPendingResult(firstScheduler);
      model.Init(secondScheduler, configuration, new[] { connector });
      Assert.That(GetSubscribers(firstScheduler), Is.Null, "Changing schedulers must detach from the previous one.");
      model.Init(secondScheduler, configuration, new[] { connector });
      Assert.That(GetPendingResult(secondScheduler).GetInvocationList(), Has.Length.EqualTo(1), "Repeated initialization must not duplicate subscriptions.");
      var row = model.ConnectorGroups.Single().ConnectorViewModels.Single();
      connector.CurrentStatus = new Status { ErrorMessage = "Old scheduler" };
      oldResult(firstScheduler, connector);
      Assert.That(row.CurrentStatus.ErrorMessage, Is.EqualTo("Initial"));
      GetPendingResult(secondScheduler)(secondScheduler, connector);
      Assert.That(row.CurrentStatus.ErrorMessage, Is.EqualTo("Old scheduler"));
      model.Dispose();
      model.Dispose();
      Assert.That(GetSubscribers(secondScheduler), Is.Null);
    }

    private static void VerifyViewLifetime()
    {
      var scheduler = new ObservationScheduler();
      var connector = CreateConnector("Main");
      var configuration = new ApplicationConfiguration();
      configuration.ConnectorsConfiguration.Add(connector.Configuration);
      var window = new MainWindow(scheduler, configuration, new[] { connector });
      try
      {
        var view = (ConnectorsTreeView)window.FindName("mainTreeView");
        view.Init(scheduler, configuration, new[] { connector });
        var oldModel = (ConnectorTreeViewModel)view.DataContext;
        var oldRow = oldModel.ConnectorGroups.Single().ConnectorViewModels.Single();
        var oldResult = GetPendingResult(scheduler);
        view.Init(scheduler, configuration, new[] { connector });
        Assert.That(GetPendingResult(scheduler).GetInvocationList(), Has.Length.EqualTo(1), "Replacing the view model must release its scheduler subscription.");
        var forwardedChanges = 0;
        view.ConfigurationChanged += (sender, args) => forwardedChanges++;
        oldModel.CreateGroup("Detached");
        Assert.That(forwardedChanges, Is.Zero, "The replaced model must no longer forward events into the view.");
        ((ConnectorTreeViewModel)view.DataContext).CreateGroup("Active");
        Assert.That(forwardedChanges, Is.EqualTo(1));
        connector.CurrentStatus = new Status { ErrorMessage = "Replaced view" };
        oldResult(scheduler, connector);
        Assert.That(oldRow.CurrentStatus.ErrorMessage, Is.EqualTo("Initial"));

        var currentModel = (ConnectorTreeViewModel)view.DataContext;
        var currentRow = currentModel.ConnectorGroups.First().ConnectorViewModels.Single();
        var pending = GetPendingResult(scheduler);
        CompleteAfterUiChange(() => pending(scheduler, connector), () => window.Close());
        Assert.That(GetSubscribers(scheduler), Is.Null, "Closing the window must release its scheduler subscription.");
        Assert.That(currentRow.CurrentStatus.ErrorMessage, Is.EqualTo("Initial"), "Already queued results must not update a closed view.");
        view.Dispose();
        Assert.That(GetSubscribers(scheduler), Is.Null, "Disposal must be safe more than once.");
      }
      finally
      {
        window.Close();
      }
    }

    private static void CompleteAfterUiChange(Action result, Action change)
    {
      var dispatcher = Application.Current.Dispatcher;
      using var queued = new ManualResetEventSlim();
      DispatcherHookEventHandler onPosted = (sender, args) =>
      {
        if (!dispatcher.CheckAccess() && args.Operation.Priority == DispatcherPriority.Send)
        {
          queued.Set();
        }
      };
      dispatcher.Hooks.OperationPosted += onPosted;
      Task completion;
      try
      {
        // Hold the UI thread until the result is queued, then change the view before processing it.
        completion = Task.Run(result);
        Assert.That(queued.Wait(TimeSpan.FromSeconds(5)), Is.True, "Expected the polling result to marshal to the UI thread.");
        change();
      }
      finally
      {
        dispatcher.Hooks.OperationPosted -= onPosted;
        dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
      }

      Assert.That(completion.Wait(TimeSpan.FromSeconds(5)), Is.True);
    }

    private static Connector CreateConnector(string group)
    {
      var configuration = new ConnectorConfiguration(
        "Jenkins",
        group,
        Connector.ServerAddress,
        "https://build.example.invalid/",
        Connector.ProjectName,
        group,
        Connector.Category,
        group);
      var connector = PluginManager.Instance.GetConnector(configuration) ?? throw new AssertionException("Expected Jenkins plugin.");
      connector.CurrentStatus = new Status { ErrorMessage = "Initial" };
      return connector;
    }

    private static EventHandler<Connector>? GetSubscribers(ObservationScheduler scheduler)
    {
      var field = typeof(ObservationScheduler).GetField("StatusQueried", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new AssertionException("Expected scheduler event backing field.");
      return (EventHandler<Connector>?)field.GetValue(scheduler);
    }

    private static EventHandler<Connector> GetPendingResult(ObservationScheduler scheduler) =>
      GetSubscribers(scheduler) ?? throw new AssertionException("Expected a scheduler subscription.");
  }
}
