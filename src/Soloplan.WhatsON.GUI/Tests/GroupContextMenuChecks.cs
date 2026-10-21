// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GroupContextMenuChecks.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.ExceptionServices;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Input;
  using System.Windows.Media;
  using System.Windows.Threading;
  using NUnit.Framework;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  internal static class GroupContextMenuChecks
  {
    internal static void Verify(App app)
    {
      foreach (var theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark })
      {
        app.ApplyTheme(theme);
        var configuration = new ApplicationConfiguration { ThemeMode = theme };
        var connectors = new List<Connector>();
        for (var index = 0; index < 120; index++)
        {
          var settings = new ConnectorConfiguration("Jenkins", $"Job {index}",
            Connector.ServerAddress, "https://build.example.invalid/", Connector.ProjectName, $"job-{index}",
            Connector.Category, $"Group {index / 40}");
          configuration.ConnectorsConfiguration.Add(settings);
          var connector = PluginManager.Instance.GetConnector(settings) ?? throw new AssertionException("Expected Jenkins plugin.");
          connector.CurrentStatus = new Status { State = ObservationState.Success };
          connectors.Add(connector);
        }

        foreach (var groupName in new[] { "Group 0", "Group 1", "Group 2", "Empty" })
        {
          configuration.Groups.Add(groupName);
        }

        var window = new MainWindow(new ObservationScheduler(), configuration, connectors)
        {
          Left = -20000, Top = -20000, Width = 800, Height = 450, ShowActivated = false, IsTreeInitialized = true,
        };
        var invalidCasts = new List<Exception>();
        EventHandler<FirstChanceExceptionEventArgs> observe = (sender, args) =>
        {
          if (args.Exception is InvalidCastException && args.Exception.StackTrace?.Contains("OnTreeItemRightMouseDown", StringComparison.Ordinal) == true)
          {
            invalidCasts.Add(args.Exception);
          }
        };
        AppDomain.CurrentDomain.FirstChanceException += observe;
        try
        {
          var control = (ConnectorsTreeView)window.FindName("mainTreeView");
          control.Init(new ObservationScheduler(), configuration, connectors);
          var tree = (TreeView)control.FindName("mainTreeView");
          foreach (var group in tree.Items.Cast<ConnectorGroupViewModel>())
          {
            group.IsNodeExpanded = false;
          }

          window.Show();
          Flush(window);
          foreach (var groupIndex in new[] { 2, 0, 1, 2 })
          {
            var item = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(groupIndex);
            var group = (ConnectorGroupViewModel)item.DataContext;
            var row = (Border)item.Template.FindName("Row", item);
            Assert.That(row.ContextMenu, Is.Not.Null, $"Group {groupIndex} must attach a menu when its header is first created.");
            Assert.That(group.ConnectorViewModels.Count, Is.EqualTo(40));
            foreach (var point in new[]
            {
              new Point(2, row.ActualHeight / 2),
              new Point(12, row.ActualHeight / 2),
              new Point(50, row.ActualHeight / 2),
              new Point(row.ActualWidth / 2, row.ActualHeight / 2),
              new Point(row.ActualWidth - 20, row.ActualHeight / 2),
              new Point(row.ActualWidth - 3, row.ActualHeight / 2),
            })
            {
              var hit = row.InputHitTest(point) as DependencyObject;
              Assert.That(hit, Is.Not.Null, $"Group {groupIndex}, header point {point}, size {row.RenderSize}.");
              Assert.That(FindMenuOwner(hit), Is.SameAs(row), "Padding, expander, and blank header space must open the same group menu.");
              if (hit is UIElement input)
              {
                input.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right)
                {
                  RoutedEvent = UIElement.MouseRightButtonDownEvent,
                });
              }
            }

            var menu = row.ContextMenu ?? throw new AssertionException("Missing group menu.");
            menu.PlacementTarget = row;
            menu.IsOpen = true;
            Flush(window);
            try
            {
              Assert.That(menu.IsVisible, Is.True);
              Assert.That(menu.DataContext, Is.SameAs(group));
              Assert.That(((MenuItem)menu.Items[0]).Command, Is.SameAs(group.RenameCommand));
              Assert.That(((MenuItem)menu.Items[1]).Command, Is.SameAs(group.DeleteCommand));
              Assert.That(((MenuItem)menu.Items[2]).Command, Is.SameAs(group.ExportCommand));
              connectors[groupIndex * 40].CurrentStatus = new Status { State = ObservationState.Running, Building = true };
              group.Update(connectors[groupIndex * 40]);
              Flush(window);
              Assert.That(menu.IsOpen, Is.True, "A build-status update must not dismiss the group menu.");
              Assert.That(menu.DataContext, Is.SameAs(group));
            }
            finally
            {
              menu.IsOpen = false;
            }

            if (groupIndex == 1)
            {
              ProjectListChecks.VerifyAddJobToGroup(window, row, group, configuration);
            }

            item.IsExpanded = true;
            Flush(window);
            var child = (TreeViewItem)item.ItemContainerGenerator.ContainerFromIndex(0);
            var childRow = (Border)child.Template.FindName("Row", child);
            var jobMenu = childRow.ContextMenu ?? throw new AssertionException("Missing full-row job menu.");
            Assert.That(jobMenu, Is.Not.SameAs(menu), "Group menus must not cover child job rows.");
            jobMenu.PlacementTarget = childRow;
            jobMenu.IsOpen = true;
            Flush(window);
            try
            {
              Assert.That(jobMenu.DataContext, Is.SameAs(child.DataContext), "The row menu must target the clicked job.");
            }
            finally
            {
              jobMenu.IsOpen = false;
            }

            item.IsExpanded = false;
            Flush(window);
          }

          var emptyItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(3);
          var emptyGroup = (ConnectorGroupViewModel)emptyItem.DataContext;
          Assert.That(emptyGroup.ConnectorViewModels, Is.Empty);
          ProjectListChecks.VerifyAddJobToGroup(window, (Border)emptyItem.Template.FindName("Row", emptyItem), emptyGroup, configuration);
          Assert.That(invalidCasts, Is.Empty, "Right-clicking a group must not cast it to a job view model.");
        }
        finally
        {
          AppDomain.CurrentDomain.FirstChanceException -= observe;
          window.Close();
        }
      }
    }

    private static FrameworkElement? FindMenuOwner(DependencyObject? element)
    {
      while (element != null)
      {
        if (element is FrameworkElement { ContextMenu: not null } owner)
        {
          return owner;
        }

        element = VisualTreeHelper.GetParent(element);
      }

      return null;
    }

    private static void Flush(Window window)
    {
      window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
      window.UpdateLayout();
    }
  }
}
