// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GroupPersistenceTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Media;
  using System.Windows.Threading;
  using Newtonsoft.Json;
  using NUnit.Framework;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.Model;
  using TreeConnectorViewModel = Soloplan.WhatsON.GUI.Common.ConnectorTreeView.ConnectorViewModel;

  [TestFixture]
  [NonParallelizable]
  public class GroupPersistenceTests
  {
    [Test]
    public void EmptyGroupsSurviveSaveReloadRenameAndExplicitDeletion()
    {
      var configuration = new ApplicationConfiguration();
      using var tree = new ConnectorTreeViewModel();
      tree.Update(configuration);
      tree.CreateGroup("Planned");
      tree.CreateGroup("Delivery");
      tree.WriteToConfiguration(configuration);

      var restored = RoundTrip(configuration);
      using var reopened = new ConnectorTreeViewModel();
      reopened.Update(restored);
      Assert.That(reopened.ConnectorGroups.Select(group => group.GroupName), Is.EqualTo(new[] { "Planned", "Delivery" }));
      Assert.That(reopened.ConnectorGroups.SelectMany(group => group.ConnectorViewModels), Is.Empty);

      reopened.ConnectorGroups[0].GroupName = "Next release";
      reopened.WriteToConfiguration(restored);
      reopened.Update(RoundTrip(restored));
      Assert.That(reopened.ConnectorGroups.Select(group => group.GroupName), Is.EqualTo(new[] { "Next release", "Delivery" }));
      reopened.DeleteGroupAsync(null, new DeleteTreeItemEventArgs(reopened.ConnectorGroups[0]));
      reopened.WriteToConfiguration(restored);
      reopened.Update(RoundTrip(restored));
      Assert.That(reopened.ConnectorGroups.Select(group => group.GroupName), Is.EqualTo(new[] { "Delivery" }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DeletingTheLastProjectsPreservesNamedGroups(bool multipleSelection)
    {
      var configuration = new ApplicationConfiguration();
      configuration.Groups.Add("Reserved");
      configuration.ConnectorsConfiguration.Add(CreateProject("Main", "Delivery"));
      if (multipleSelection)
      {
        configuration.ConnectorsConfiguration.Add(CreateProject("Installer", "Delivery"));
      }

      using var tree = new ConnectorTreeViewModel();
      tree.Update(configuration);
      var delivery = tree.ConnectorGroups.Single(group => group.GroupName == "Delivery");
      if (multipleSelection)
      {
        tree.UpdateSelectedConnectors(new Collection<TreeConnectorViewModel>(delivery.ConnectorViewModels.ToList()));
      }

      tree.DeleteGroupAsync(null, new DeleteTreeItemEventArgs(delivery.ConnectorViewModels.First()));
      tree.WriteToConfiguration(configuration);
      tree.Update(RoundTrip(configuration));

      Assert.That(tree.ConnectorGroups.Select(group => group.GroupName), Is.EqualTo(new[] { "Reserved", "Delivery" }));
      Assert.That(configuration.ConnectorsConfiguration, Is.Empty);
      Assert.That(tree.ConnectorGroups.SelectMany(group => group.ConnectorViewModels), Is.Empty);
    }

    [Test]
    public void DeletingTheLastUngroupedProjectDoesNotLeaveAnEmptyUnnamedSection()
    {
      var configuration = new ApplicationConfiguration();
      configuration.ConnectorsConfiguration.Add(CreateProject("Main", string.Empty));
      using var tree = new ConnectorTreeViewModel();
      tree.Update(configuration);
      tree.DeleteGroupAsync(null, new DeleteTreeItemEventArgs(tree.ConnectorGroups.Single().ConnectorViewModels.Single()));
      tree.WriteToConfiguration(configuration);

      Assert.That(tree.ConnectorGroups, Is.Empty);
      Assert.That(configuration.Groups, Is.Empty);
      Assert.That(configuration.ConnectorsConfiguration, Is.Empty);
    }

    [Test]
    public void PreparedGroupCanBePopulatedAndItsOrderSurvivesReload()
    {
      var configuration = new ApplicationConfiguration();
      configuration.Groups.Add("Planned");
      configuration.Groups.Add("Delivery");
      using var tree = new ConnectorTreeViewModel();
      tree.Update(configuration);
      var prepared = tree.ConnectorGroups.First();
      configuration.ConnectorsConfiguration.Add(CreateProject("Main", "Planned"));
      tree.Update(configuration);

      Assert.That(tree.ConnectorGroups.First(), Is.SameAs(prepared));
      Assert.That(prepared.ConnectorViewModels.Single().Name, Is.EqualTo("Main"));
      tree.ConnectorGroups.Move(0, 1);
      tree.WriteToConfiguration(configuration);
      tree.Update(RoundTrip(configuration));
      Assert.That(tree.ConnectorGroups.Select(group => group.GroupName), Is.EqualTo(new[] { "Delivery", "Planned" }));
    }

    [Test]
    public void UngroupedProjectsKeepTheirPositionBetweenSavedNamedGroups()
    {
      var configuration = new ApplicationConfiguration();
      configuration.ConnectorsConfiguration.Add(CreateProject("First", "Delivery"));
      configuration.ConnectorsConfiguration.Add(CreateProject("Ungrouped", string.Empty));
      configuration.ConnectorsConfiguration.Add(CreateProject("Last", "Planned"));
      using var tree = new ConnectorTreeViewModel();
      tree.Update(configuration);
      tree.WriteToConfiguration(configuration);
      var settings = new ConfigViewModel();
      settings.Load(RoundTrip(configuration));
      tree.Update(RoundTrip(configuration));

      Assert.That(tree.ConnectorGroups.Select(group => group.GroupName), Is.EqualTo(new[] { "Delivery", string.Empty, "Planned" }));
      Assert.That(settings.Groups, Is.EqualTo(new[] { "Delivery", string.Empty, "Planned" }));
    }

    [Test]
    public void SettingsKeepEmptyAndLegacyGroupsWhenDeletingProjectsAndExporting()
    {
      var configuration = new ApplicationConfiguration();
      configuration.Groups.Add("Reserved");
      configuration.ConnectorsConfiguration.Add(CreateProject("Main", "Delivery"));
      var settings = new ConfigViewModel();
      settings.Load(configuration);
      settings.Connectors.Clear();
      GlobalConfigDataViewModel.Instance.UseConfiguration(settings);
      try
      {
        Assert.That(GlobalConfigDataViewModel.Instance.UsedCategories, Is.EqualTo(new[] { "Reserved", "Delivery" }));
        var exported = new ApplicationConfiguration();
        settings.ApplyToConfiguration(exported);
        Assert.That(RoundTrip(exported).Groups, Is.EqualTo(new[] { "Reserved", "Delivery" }));
        Assert.That(exported.ConnectorsConfiguration, Is.Empty);
      }
      finally
      {
        GlobalConfigDataViewModel.Instance.UseConfiguration(new ConfigViewModel());
      }
    }

    internal static void VerifyEmptyGroupHeaders()
    {
      var configuration = new ApplicationConfiguration();
      configuration.Groups.Add("Planned");
      configuration.Groups.Add("Delivery");
      var scheduler = new ObservationScheduler();
      var window = new MainWindow(scheduler, configuration, Array.Empty<Connector>())
      {
        Left = -20000, Top = -20000, Width = 800, Height = 450, ShowActivated = false, IsTreeInitialized = true,
      };
      try
      {
        var control = (ConnectorsTreeView)window.FindName("mainTreeView");
        control.Init(scheduler, configuration, Array.Empty<Connector>());
        window.Show();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
        var tree = (TreeView)control.FindName("mainTreeView");
        Assert.That(tree.Items, Has.Count.EqualTo(2));
        for (var index = 0; index < tree.Items.Count; index++)
        {
          var item = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(index);
          var row = (Border)item.Template.FindName("Row", item);
          Assert.That(item.ActualHeight, Is.GreaterThan(0), "Empty groups must still have visible headers.");
          Assert.That(row.ContextMenu, Is.Not.Null, "Empty groups must remain available for renaming or explicit deletion.");
        }
      }
      finally
      {
        window.Close();
      }
    }

    internal static void VerifyGroupCreation()
    {
      foreach (var initialState in new[] { "Empty", "Named group", "Ungrouped project" })
      {
        var configuration = new ApplicationConfiguration();
        if (initialState == "Named group")
        {
          configuration.Groups.Add("Existing");
        }
        else if (initialState == "Ungrouped project")
        {
          configuration.ConnectorsConfiguration.Add(CreateProject("Main", string.Empty));
        }

        var scheduler = new ObservationScheduler();
        var window = new MainWindow(scheduler, configuration, Array.Empty<Connector>())
        {
          Left = -20000, Top = -20000, Width = 800, Height = 450, ShowActivated = false, IsTreeInitialized = true,
        };
        try
        {
          var control = (ConnectorsTreeView)window.FindName("mainTreeView");
          control.Init(scheduler, configuration, Array.Empty<Connector>());
          window.Show();
          window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
          window.UpdateLayout();
          var model = (ConnectorTreeViewModel)control.DataContext;
          var originalCount = model.ConnectorGroups.Count;
          var tree = (TreeView)control.FindName("mainTreeView");
          control.CreateGroup("Test");
          window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
          window.UpdateLayout();

          Assert.That(model.ConnectorGroups, Has.Count.EqualTo(originalCount + 1), initialState);
          Assert.That(tree.Items, Has.Count.EqualTo(originalCount + 1), initialState);
          Assert.That(
            GetRows(tree).Count(row => row.DataContext is ConnectorGroupViewModel),
            Is.EqualTo(originalCount + 1),
            $"Creating one group must produce one visible header ({initialState}).");
        }
        finally
        {
          window.Close();
        }
      }
    }

    private static IEnumerable<TreeViewItem> GetRows(DependencyObject parent)
    {
      for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
      {
        var child = VisualTreeHelper.GetChild(parent, index);
        if (child is TreeViewItem row)
        {
          yield return row;
        }

        foreach (var descendant in GetRows(child))
        {
          yield return descendant;
        }
      }
    }

    private static ConnectorConfiguration CreateProject(string name, string group) =>
      new ConnectorConfiguration("UnavailablePlugin", name, Connector.Category, group);

    private static ApplicationConfiguration RoundTrip(ApplicationConfiguration configuration) =>
      JsonConvert.DeserializeObject<ApplicationConfiguration>(JsonConvert.SerializeObject(configuration))
      ?? throw new AssertionException("Expected configuration.");
  }
}
