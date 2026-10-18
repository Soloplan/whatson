// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ToolTipChecks.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Markup;
  using System.Windows.Media;
  using System.Windows.Threading;
  using NUnit.Framework;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  internal static class ToolTipChecks
  {
    internal static void Verify(App app)
    {
      var previousCulture = CultureInfo.CurrentUICulture;
      var target = new Button { Content = "Project" };
      var host = new Window { Content = target, Left = -20000, Top = -20000, Width = 300, Height = 200, ShowActivated = false };
      try
      {
        host.Show();
        foreach (var culture in new[] { "en-US", "de-DE" })
        {
          CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
          foreach (var theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark })
          {
            app.ApplyTheme(theme);
            CheckPopup(new ToolTip { Content = "release/image" }, target, app);
            CheckPopup((ToolTip)new BuildNumberControl().ToolTip, target, app);
            foreach (var plugin in PluginManager.Instance.GetPresentationPlugins())
            {
              var configuration = new ConnectorConfiguration(
                plugin.ConnectorType,
                "release/image",
                [
                  new ConfigurationItem(Connector.ServerAddress, "https://build.example.invalid/"),
                  new ConfigurationItem(Connector.DirectAddress, "https://build.example.invalid/"),
                  new ConfigurationItem(Connector.ProjectName, "release/image"),
                ]);
              var connector = PluginManager.Instance.GetConnector(configuration) ?? throw new AssertionException("Expected connector plugin was not found.");
              connector.CurrentStatus = new Status { State = ObservationState.Unknown, BuildNumber = 108, Time = DateTime.UtcNow.AddHours(-1), Duration = TimeSpan.FromMinutes(3) };
              var model = plugin.CreateViewModel(connector);
              model.Update(connector);
              model.CurrentStatus?.Culprits.Add(new UserViewModel { FullName = "A contributor with a very long display name that should wrap inside the tooltip instead of overflowing" });
              using (var reader = plugin.GetDataTempletXaml())
              {
                var resources = (ResourceDictionary)XamlReader.Load(reader);
                foreach (var tooltip in resources.Values.OfType<ToolTip>())
                {
                  tooltip.DataContext = tooltip.Content is StackPanel panel && panel.GetBindingExpression(FrameworkElement.DataContextProperty) != null
                    ? model : model.CurrentStatus;
                  CheckPopup(tooltip, target, app);
                  var caption = Descendants<TextBlock>(tooltip).FirstOrDefault(text => text.Text == (plugin.ConnectorType == "Jenkins" ? Strings.BuildContributorsCaption : Strings.ChangesByCaption));
                  Assert.That(caption, Is.Not.Null, "Contributor captions follow the interface language in both plugins.");
                }
              }
            }

            var status = new BuildStatusViewModel(new ConnectorViewModel());
            status.Update(new Status { State = ObservationState.Success, BuildNumber = 108, Time = DateTime.UtcNow.AddHours(-1), Duration = TimeSpan.FromMinutes(3) });
            var history = new HistoryBuildTooltipControl { DataContext = status };
            CheckPopup(new ToolTip { Content = history }, target, app);
            var emptyContributors = new CulpritsControl { CulpritsProp = status.Culprits, Caption = Strings.BuildContributorsCaption };
            emptyContributors.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            emptyContributors.Measure(new Size(400, double.PositiveInfinity));
            Assert.That(emptyContributors.DesiredSize.Height, Is.Zero, "Empty contributor sections must not reserve space.");
            status.Culprits.Add(new UserViewModel { FullName = "Build contributor" });
            emptyContributors.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            emptyContributors.Measure(new Size(400, double.PositiveInfinity));
            Assert.That(emptyContributors.DesiredSize.Height, Is.GreaterThan(0));
            status.Culprits.Clear();
            emptyContributors.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            emptyContributors.Measure(new Size(400, double.PositiveInfinity));
            Assert.That(emptyContributors.DesiredSize.Height, Is.Zero, "Removing contributors must also remove section spacing.");
            foreach (var minutes in new[] { 3, 10 })
            {
              status.Update(new Status { State = ObservationState.Running, Building = true, Duration = TimeSpan.FromMinutes(minutes), EstimatedDuration = TimeSpan.FromMinutes(8) });
              CheckPopup(new ToolTip { Content = new ProgressBarTooltipControl { DataContext = status } }, target, app);
            }
          }
        }
      }
      finally
      {
        host.Close();
        CultureInfo.CurrentUICulture = previousCulture;
      }
    }

    private static void CheckPopup(ToolTip tooltip, UIElement target, App app)
    {
      tooltip.PlacementTarget = target;
      try
      {
        tooltip.IsOpen = true;
        tooltip.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        tooltip.UpdateLayout();
        Assert.That(tooltip.Background, Is.SameAs(app.Resources["SettingsSurface"]));
        Assert.That(tooltip.Foreground, Is.SameAs(app.Resources["MaterialDesignBody"]));
        Assert.That(tooltip.BorderBrush, Is.SameAs(app.Resources["SettingsBorder"]));
        Assert.That(tooltip.ActualWidth, Is.InRange(1, 440));
        var text = Descendants<TextBlock>(tooltip).Where(block => block.IsVisible && !string.IsNullOrEmpty(block.Text)).ToList();
        Assert.That(text, Is.Not.Empty);
        Assert.That(text.All(block => block.Foreground == tooltip.Foreground || block.Foreground == app.Resources["SettingsMuted"]), Is.True);
        var lastTextBottom = text.Max(block => block.TranslatePoint(new Point(0, block.ActualHeight), tooltip).Y);
        Assert.That(tooltip.ActualHeight - lastTextBottom, Is.InRange(9, 14), "Tooltip content must not end with blank spacer rows.");
        var dark = ((SolidColorBrush)tooltip.Background).Color.R < 100;
        app.ApplyTheme(dark ? ApplicationTheme.Light : ApplicationTheme.Dark);
        Assert.That(tooltip.Background, Is.SameAs(app.Resources["SettingsSurface"]), "Open tooltips follow theme changes.");
        app.ApplyTheme(dark ? ApplicationTheme.Dark : ApplicationTheme.Light);
      }
      finally
      {
        tooltip.IsOpen = false;
      }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent)
      where T : DependencyObject
    {
      for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
      {
        var child = VisualTreeHelper.GetChild(parent, index);
        if (child is T match)
        {
          yield return match;
        }

        foreach (var descendant in Descendants<T>(child))
        {
          yield return descendant;
        }
      }
    }
  }
}
