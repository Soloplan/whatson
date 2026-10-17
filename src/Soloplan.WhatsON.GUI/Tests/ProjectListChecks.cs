// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectListChecks.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Reflection;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Media;
  using System.Windows.Threading;
  using NUnit.Framework;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  internal static class ProjectListChecks
  {
    internal static void Verify(App app)
    {
      foreach (var theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark })
      {
        app.ApplyTheme(theme);
        foreach (var density in new[] { ViewStyle.Normal, ViewStyle.Compact, ViewStyle.Pcked })
        {
          var configuration = new ApplicationConfiguration { ThemeMode = theme, ViewStyle = density };
          var connectors = new List<Connector>();
          for (var index = 0; index < 6; index++)
          {
            var settings = new ConnectorConfiguration(
              index < 3 ? "Jenkins" : "CruiseControl",
              "Project with a long name that must yield space to build information " + index,
              new List<ConfigurationItem>
              {
                new ConfigurationItem(Connector.ServerAddress, "https://build.example.invalid/"),
                new ConfigurationItem(Connector.DirectAddress, "https://build.example.invalid/"),
                new ConfigurationItem(Connector.ProjectName, "release/main"),
                new ConfigurationItem(Connector.Category, index < 3 ? "Application" : "Delivery"),
              });
            configuration.ConnectorsConfiguration.Add(settings);
            var connector = PluginManager.Instance.GetConnector(settings);
            connector.IsDisabled = index == 0;
            connector.CurrentStatus = new Status
            {
              State = index == 1 ? ObservationState.Running : ObservationState.Success,
              Building = index == 1,
              BuildNumber = 108 + index,
              Time = DateTime.UtcNow.AddMinutes(-3),
              Duration = TimeSpan.FromMinutes(3),
              EstimatedDuration = TimeSpan.FromMinutes(8),
            };
            for (var history = 0; history < 5; history++)
            {
              connector.AddSnapshot(new Status { State = ObservationState.Success, BuildNumber = 107 - history, Time = DateTime.UtcNow.AddHours(-history - 1) });
            }

            connectors.Add(connector);
          }

          var window = new MainWindow(new ObservationScheduler(), configuration, connectors)
          {
            Left = -20000, Top = -20000, Width = 800, Height = 450, ShowActivated = false, IsTreeInitialized = true,
          };
          try
          {
            var control = (ConnectorsTreeView)window.FindName("mainTreeView");
            control.Init(new ObservationScheduler(), configuration, connectors);
            window.Show();
            var tree = (TreeView)control.FindName("mainTreeView");
            foreach (var width in new[] { 800, 420, 300 })
            {
              window.Width = width;
              window.UpdateLayout();
              window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
              var viewer = FindVisualChild<ScrollViewer>(tree);
              Assert.That(viewer.ExtentWidth, Is.LessThanOrEqualTo(viewer.ViewportWidth + 1), "Narrow windows must not require horizontal scrolling.");
              for (var groupIndex = 0; groupIndex < 2; groupIndex++)
              {
                var group = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(groupIndex);
                for (var index = 0; index < 3; index++)
                {
                  var item = (TreeViewItem)group.ItemContainerGenerator.ContainerFromIndex(index);
                  var presenter = (ContentPresenter)item.Template.FindName("PART_Header", item);
                  var row = (ProjectRowControl)VisualTreeHelper.GetChild(presenter, 0);
                  var model = (ConnectorViewModel)item.DataContext;
                  var disabled = groupIndex == 0 && index == 0;
                  Assert.That(model.IsDisabled, Is.EqualTo(disabled));
                  Assert.That(((FrameworkElement)row.FindName("DisabledIcon")).Visibility, Is.EqualTo(disabled ? Visibility.Visible : Visibility.Collapsed));
                  Assert.That(((FrameworkElement)row.FindName("BuildStateIcon")).Visibility, Is.EqualTo(disabled ? Visibility.Collapsed : Visibility.Visible));
                  var badge = (Border)row.FindName("DisabledBadge");
                  Assert.That(badge.Visibility, Is.EqualTo(disabled ? Visibility.Visible : Visibility.Collapsed));
                  Assert.That(((TextBlock)badge.Child).Text, Is.EqualTo(Strings.JobDisabled));
                  Assert.That(badge.ToolTip, Is.EqualTo(Strings.JobDisabledDescription));
                  Assert.That(row.IsEnabled, Is.True, "Disabled jobs retain their project and history actions.");
                  if (disabled)
                  {
                    Assert.That(((TextBlock)row.FindName("ProjectName")).Foreground, Is.SameAs(app.Resources["SettingsMuted"]));
                    Assert.That(badge.Background, Is.SameAs(app.Resources["SettingsBackground"]));
                  }

                  Assert.That(((BuildNumberControl)row.FindName("BuildNumber")).BuildNumber, Is.EqualTo(model.CurrentStatus.BuildNumber));
                  Assert.That(row.ContextMenu, Is.Not.Null, "Both plugins must retain project commands.");
                  Assert.That(((TextBlock)row.FindName("ProjectName")).ActualWidth, Is.GreaterThan(35), "Project names take priority over optional metadata.");
                  Assert.That(((FrameworkElement)row.FindName("BuildHistory")).Visibility, Is.EqualTo(width == 800 ? Visibility.Visible : Visibility.Collapsed));
                  Assert.That(((FrameworkElement)row.FindName("BuildTiming")).Visibility, Is.EqualTo(width == 300 ? Visibility.Collapsed : Visibility.Visible));
                  Assert.That(((ProgressBar)row.FindName("BuildProgress")).Value, Is.EqualTo(model.CurrentStatus.Progress));
                  var historyList = FindVisualChild<ItemsControl>((HistoryBuildList)row.FindName("BuildHistory"));
                  Assert.That(historyList.Items.Count, Is.EqualTo(5));
                  if (width == 800)
                  {
                    var historyControl = FindVisualChild<HistoryBuildControl>(historyList);
                    var historyMenu = FindVisualChild<DockPanel>(historyControl).ContextMenu;
                    historyMenu.DataContext = historyControl.DataContext;
                    Assert.That(((MenuItem)historyMenu.Items[0]).Command, Is.Not.Null, "History keeps the copy-build command.");
                    Assert.That(((MenuItem)historyMenu.Items[1]).Command, Is.Not.Null, "History keeps the open-build command.");
                  }
                }
              }
            }

            var firstGroup = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(0);
            firstGroup.IsExpanded = false;
            Assert.That(((ConnectorGroupViewModel)firstGroup.DataContext).IsNodeExpanded, Is.False);
            firstGroup.IsExpanded = true;
            window.UpdateLayout();
            var selectedRow = (TreeViewItem)firstGroup.ItemContainerGenerator.ContainerFromIndex(0);
            var selectedPresenter = (ContentPresenter)selectedRow.Template.FindName("PART_Header", selectedRow);
            var disabledRow = (ProjectRowControl)VisualTreeHelper.GetChild(selectedPresenter, 0);
            var disabledModel = (ConnectorViewModel)selectedRow.DataContext;
            connectors[0].IsDisabled = false;
            disabledModel.Update(connectors[0]);
            window.UpdateLayout();
            Assert.That(((FrameworkElement)disabledRow.FindName("DisabledBadge")).Visibility, Is.EqualTo(Visibility.Collapsed));
            Assert.That(((FrameworkElement)disabledRow.FindName("BuildStateIcon")).Visibility, Is.EqualTo(Visibility.Visible));
            Assert.That(disabledModel.CurrentStatus.State, Is.EqualTo(ObservationState.Success));
            Assert.That(disabledModel.ConnectorSnapshots.Count, Is.EqualTo(5));
            var toggleSelection = typeof(ConnectorsTreeView).GetMethod("OnCtrlProjectClicked", BindingFlags.Instance | BindingFlags.NonPublic);
            toggleSelection.Invoke(control, new[] { selectedRow.DataContext });
            Assert.That(selectedRow.Background, Is.SameAs(app.Resources["SettingsSelection"]));
            Assert.That(selectedRow.Foreground, Is.SameAs(app.Resources["MaterialDesignBody"]));
            var secondRow = (TreeViewItem)firstGroup.ItemContainerGenerator.ContainerFromIndex(1);
            toggleSelection.Invoke(control, new[] { secondRow.DataContext });
            Assert.That(secondRow.Background, Is.SameAs(selectedRow.Background), "Ctrl-selection keeps both rows highlighted.");
            app.ApplyTheme(theme == ApplicationTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark);
            Assert.That(selectedRow.Background, Is.SameAs(app.Resources["SettingsSelection"]), "Selection follows live theme changes.");
            toggleSelection.Invoke(control, new[] { selectedRow.DataContext });
            Assert.That(selectedRow.ReadLocalValue(Control.BackgroundProperty), Is.EqualTo(DependencyProperty.UnsetValue));
            Assert.That(secondRow.Background, Is.SameAs(app.Resources["SettingsSelection"]));
            toggleSelection.Invoke(control, new[] { secondRow.DataContext });
            app.ApplyTheme(theme);
          }
          finally
          {
            window.Close();
          }
        }
      }
    }

    private static T FindVisualChild<T>(DependencyObject parent)
      where T : DependencyObject
    {
      for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
      {
        var child = VisualTreeHelper.GetChild(parent, index);
        if (child is T match)
        {
          return match;
        }

        var descendant = FindVisualChild<T>(child);
        if (descendant != null)
        {
          return descendant;
        }
      }

      return null;
    }
  }
}
