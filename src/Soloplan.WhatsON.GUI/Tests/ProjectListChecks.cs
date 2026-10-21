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
  using System.IO;
  using System.Linq;
  using System.Reflection;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Media;
  using System.Windows.Media.Imaging;
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
              [
                new ConfigurationItem(Connector.ServerAddress, "https://build.example.invalid/"),
                new ConfigurationItem(Connector.DirectAddress, "https://build.example.invalid/"),
                new ConfigurationItem(Connector.ProjectName, "release/main"),
                new ConfigurationItem(Connector.Category, index < 3 ? "Application" : "Delivery"),
              ]);
            configuration.ConnectorsConfiguration.Add(settings);
            var connector = PluginManager.Instance.GetConnector(settings) ?? throw new AssertionException("Expected connector plugin was not found.");
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
            if (index == 2)
            {
              connector.CurrentStatus = new Soloplan.WhatsON.Jenkins.JenkinsStatus(ObservationState.Failure)
              {
                BuildNumber = 110,
                Details = Soloplan.WhatsON.Jenkins.JenkinsVolunteer.SetOwner(null, "alexander.long.username"),
                Time = DateTime.UtcNow.AddMinutes(-3),
              };
            }

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
              var viewer = FindVisualChild<ScrollViewer>(tree) ?? throw new AssertionException("Missing scroll viewer.");
              var root = (FrameworkElement)window.Content;
              var viewerRight = viewer.TransformToAncestor(root).Transform(new Point(viewer.ActualWidth, 0)).X;
              Assert.That(root.ActualWidth - viewerRight, Is.EqualTo(8).Within(1), "The build list and settings must use the same scrollbar inset.");
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

                  Assert.That(((BuildNumberControl)row.FindName("BuildNumber")).BuildNumber, Is.EqualTo(model.CurrentStatus?.BuildNumber));
                  Assert.That(row.ContextMenu, Is.Not.Null, "Both plugins must retain project commands.");
                  if (groupIndex == 0 && index == 2 && width == 800)
                  {
                    row.ContextMenu.PlacementTarget = row;
                    row.ContextMenu.DataContext = model;
                    row.ContextMenu.IsOpen = true;
                    try
                    {
                      window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                      var current = model.CurrentStatus ?? throw new AssertionException("Expected Jenkins status.");
                      var volunteerMenu = row.ContextMenu.Items.OfType<MenuItem>()
                        .Single(item => item.Command == current.VolunteerToFix || item.Command == current.ClearVolunteer);
                      Assert.That(row.ContextMenu.Items[0], Is.SameAs(volunteerMenu), "The volunteer action goes first.");
                      Assert.That(row.ContextMenu.Items[1], Is.TypeOf<MenuItem>());
                      Assert.That(((MenuItem)row.ContextMenu.Items[1]).Visibility, Is.EqualTo(Visibility.Collapsed));
                      Assert.That(row.ContextMenu.Items[2], Is.TypeOf<Separator>());
                      Assert.That(volunteerMenu.Visibility, Is.EqualTo(Visibility.Visible), "Missing credentials must not hide the action.");
                      Assert.That(ToolTipService.GetShowOnDisabled(volunteerMenu), Is.True, "The explanation must be available while disabled.");
                      Assert.That(FindVisualChild<MaterialDesignThemes.Wpf.PackIcon>(volunteerMenu)?.Kind, Is.EqualTo(MaterialDesignThemes.Wpf.PackIconKind.AccountWrench));
                      Assert.That(volunteerMenu.IsEnabled, Is.False, "These projects have no Jenkins credentials.");
                      Assert.That(volunteerMenu.Header, Is.EqualTo(Strings.ClearVolunteer));
                      Assert.That(volunteerMenu.ToolTip, Is.EqualTo(Strings.ClearVolunteerDescription));
                      Assert.That(volunteerMenu.Command, Is.SameAs(current.ClearVolunteer));

                      current.Update(new Soloplan.WhatsON.Jenkins.JenkinsStatus(ObservationState.Failure) { BuildNumber = 110 });
                      window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                      Assert.That(volunteerMenu.Header, Is.EqualTo(Strings.VolunteerToFix));
                      Assert.That(volunteerMenu.ToolTip, Is.EqualTo(Strings.VolunteerToFixDescription));
                      Assert.That(volunteerMenu.Command, Is.SameAs(current.VolunteerToFix));
                      Assert.That(volunteerMenu.IsEnabled, Is.False, "The claim action also stays disabled without credentials.");

                      connectors[2].Configuration.GetConfigurationByKey(Soloplan.WhatsON.Jenkins.JenkinsConnector.UserName).Value = "alex";
                      connectors[2].Configuration.GetConfigurationByKey(Soloplan.WhatsON.Jenkins.JenkinsConnector.ApiToken).Value = "preview-only";
                      current.Update(new Soloplan.WhatsON.Jenkins.JenkinsStatus(ObservationState.Failure) { BuildNumber = 110 });
                      window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                      Assert.That(volunteerMenu.IsEnabled, Is.True);
                      current.Update(connectors[2].CurrentStatus);
                      window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                      Assert.That(volunteerMenu.Header, Is.EqualTo(Strings.ClearVolunteer));
                      Assert.That(volunteerMenu.ToolTip, Is.EqualTo(Strings.ClearVolunteerDescription));
                      Assert.That(volunteerMenu.Command, Is.SameAs(current.ClearVolunteer));
                      Assert.That(volunteerMenu.IsEnabled, Is.True);
                      current.Update(new Soloplan.WhatsON.Jenkins.JenkinsStatus(ObservationState.Failure)
                      {
                        BuildNumber = 110,
                        ActiveFixes = new[] { new Soloplan.WhatsON.Jenkins.JenkinsFix(34, "rHr") },
                      });
                      window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                      var assignmentsMenu = (MenuItem)row.ContextMenu.Items[1];
                      Assert.That(assignmentsMenu.Visibility, Is.EqualTo(Visibility.Visible));
                      Assert.That(((Border)row.FindName("VolunteerBadge")).Visibility, Is.EqualTo(Visibility.Visible));
                      assignmentsMenu.IsSubmenuOpen = true;
                      window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                      var assignmentMenu = (MenuItem)assignmentsMenu.ItemContainerGenerator.ContainerFromIndex(0);
                      Assert.That(assignmentMenu.Header, Is.EqualTo("RHR (#34)"));
                      Assert.That(assignmentMenu.Command, Is.SameAs(current.VolunteerAssignments[0].ClearVolunteer));
                      Assert.That(assignmentMenu.IsEnabled, Is.True);
                      assignmentsMenu.IsSubmenuOpen = false;
                      connectors[2].Configuration.GetConfigurationByKey(Soloplan.WhatsON.Jenkins.JenkinsConnector.UserName).Value = string.Empty;
                      connectors[2].Configuration.GetConfigurationByKey(Soloplan.WhatsON.Jenkins.JenkinsConnector.ApiToken).Value = string.Empty;
                      current.Update(connectors[2].CurrentStatus);
                    }
                    finally
                    {
                      row.ContextMenu.IsOpen = false;
                    }
                  }

                  Assert.That(((Border)row.FindName("VolunteerBadge")).Visibility, Is.EqualTo(groupIndex == 0 && index == 2 ? Visibility.Visible : Visibility.Collapsed));
                  if (groupIndex == 0 && index == 2)
                  {
                    var volunteerBadge = (Border)row.FindName("VolunteerBadge");
                    var volunteerName = (TextBlock)row.FindName("VolunteerName");
                    var projectName = (TextBlock)row.FindName("ProjectName");
                    Assert.That(volunteerName.Text, Is.EqualTo("ALEXANDER.LONG.USERNAME"));
                    Assert.That(volunteerName.Foreground, Is.SameAs(app.Resources["SettingsAccent"]));
                    var badgeCenter = volunteerBadge.TransformToAncestor(row).Transform(new Point(0, volunteerBadge.ActualHeight / 2)).Y;
                    var nameCenter = projectName.TransformToAncestor(row).Transform(new Point(0, projectName.ActualHeight / 2)).Y;
                    Assert.That(badgeCenter, Is.EqualTo(nameCenter).Within(1), "The volunteer belongs on the same line as the project name.");
                    if (width == 800)
                    {
                      var bitmap = new RenderTargetBitmap((int)Math.Ceiling(row.ActualWidth), (int)Math.Ceiling(row.ActualHeight), 96, 96, PixelFormats.Pbgra32);
                      bitmap.Render(row);
                      var encoder = new PngBitmapEncoder();
                      encoder.Frames.Add(BitmapFrame.Create(bitmap));
                      var preview = Path.Combine(TestContext.CurrentContext.TestDirectory, $"VolunteerRow-{theme}-{density}.png");
                      using (var stream = File.Create(preview))
                      {
                        encoder.Save(stream);
                      }

                      TestContext.AddTestAttachment(preview);
                    }
                  }
                  Assert.That(((TextBlock)row.FindName("ProjectName")).ActualWidth, Is.GreaterThan(35), "Project names take priority over optional metadata.");
                  Assert.That(((FrameworkElement)row.FindName("BuildHistory")).Visibility, Is.EqualTo(width == 800 ? Visibility.Visible : Visibility.Collapsed));
                  Assert.That(((FrameworkElement)row.FindName("BuildTiming")).Visibility, Is.EqualTo(width == 300 ? Visibility.Collapsed : Visibility.Visible));
                  Assert.That(((ProgressBar)row.FindName("BuildProgress")).Value, Is.EqualTo(model.CurrentStatus?.Progress));
                  var historyList = FindVisualChild<ItemsControl>((HistoryBuildList)row.FindName("BuildHistory")) ?? throw new AssertionException("Missing history list.");
                  Assert.That(historyList.Items.Count, Is.EqualTo(5));
                  if (width == 800)
                  {
                    var historyControl = FindVisualChild<HistoryBuildControl>(historyList) ?? throw new AssertionException("Missing history control.");
                    var historyMenu = (FindVisualChild<DockPanel>(historyControl) ?? throw new AssertionException("Missing history panel.")).ContextMenu;
                    historyMenu.DataContext = historyControl.DataContext;
                    Assert.That(((MenuItem)historyMenu.Items[2]).Command, Is.Not.Null, "History keeps the copy-build command.");
                    Assert.That(((MenuItem)historyMenu.Items[3]).Command, Is.Not.Null, "History keeps the open-build command.");
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
            Assert.That(disabledModel.CurrentStatus?.State, Is.EqualTo(ObservationState.Success));
            Assert.That(disabledModel.ConnectorSnapshots.Count, Is.EqualTo(5));
            var toggleSelection = typeof(ConnectorsTreeView).GetMethod("OnCtrlProjectClicked", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Expected member was not found.");
            toggleSelection.Invoke(control, [selectedRow.DataContext]);
            Assert.That(selectedRow.Background, Is.SameAs(app.Resources["SettingsSelection"]));
            Assert.That(selectedRow.Foreground, Is.SameAs(app.Resources["MaterialDesignBody"]));
            var secondRow = (TreeViewItem)firstGroup.ItemContainerGenerator.ContainerFromIndex(1);
            toggleSelection.Invoke(control, [secondRow.DataContext]);
            Assert.That(secondRow.Background, Is.SameAs(selectedRow.Background), "Ctrl-selection keeps both rows highlighted.");
            app.ApplyTheme(theme == ApplicationTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark);
            Assert.That(selectedRow.Background, Is.SameAs(app.Resources["SettingsSelection"]), "Selection follows live theme changes.");
            toggleSelection.Invoke(control, [selectedRow.DataContext]);
            Assert.That(selectedRow.ReadLocalValue(Control.BackgroundProperty), Is.EqualTo(DependencyProperty.UnsetValue));
            Assert.That(secondRow.Background, Is.SameAs(app.Resources["SettingsSelection"]));
            toggleSelection.Invoke(control, [secondRow.DataContext]);
            app.ApplyTheme(theme);
          }
          finally
          {
            window.Close();
          }
        }
      }
    }

    private static T? FindVisualChild<T>(DependencyObject parent)
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
