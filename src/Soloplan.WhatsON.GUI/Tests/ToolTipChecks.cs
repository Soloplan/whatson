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
  using System.IO;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Markup;
  using System.Windows.Media;
  using System.Windows.Media.Imaging;
  using System.Windows.Threading;
  using NUnit.Framework;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Jenkins.GUI;
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
                "Integration",
                [
                  new ConfigurationItem(Connector.ServerAddress, "https://build.example.invalid/"),
                  new ConfigurationItem(Connector.DirectAddress, "https://build.example.invalid/"),
                  new ConfigurationItem(Connector.ProjectName, "sample-project/integration"),
                ]);
              var connector = PluginManager.Instance.GetConnector(configuration) ?? throw new AssertionException("Expected connector plugin was not found.");
              connector.CurrentStatus = new JenkinsStatus(ObservationState.Unstable)
              {
                FailedTestCount = 4,
                SkippedTestCount = 1,
                FailureDetails = "Example.Product.Integration.Tests.SampleConfigurationStoreTests.ReadTestStructureReturnsSampleStructureOfAllTestDataItemsAsync\nExample.Product.Integration.Tests.SampleConfigurationStoreTests.SerializeSampleStructureOfAllTestDataItemsAsync\nExample.Product.Tests.LayoutTests.ThirdAsync\nExample.Product.Tests.LayoutTests.FourthAsync\nSKIPPED: Example.Product.Tests.OtherTests.FifthAsync",
                BuildNumber = 142,
                Time = DateTime.UtcNow.AddHours(-1),
                Duration = TimeSpan.FromMinutes(18) + TimeSpan.FromSeconds(3),
              };
              var model = plugin.CreateViewModel(connector);
              model.Update(connector);
              for (var person = 0; person < 36; person++)
              {
                model.CurrentStatus?.Culprits.Add(new UserViewModel { FullName = "Build contributor " + person });
              }

              if (model.CurrentStatus is JenkinsStatusViewModel jenkins)
              {
                for (var person = 0; person < 3; person++)
                {
                  jenkins.CommittedToThisBuild.Add(new UserViewModel { FullName = "Committer " + person });
                }

                jenkins.CulpritsAndLastCommittedDifferent = true;
              }
              using (var reader = plugin.GetDataTempletXaml())
              {
                var resources = (ResourceDictionary)XamlReader.Load(reader);
                foreach (var tooltip in resources.Values.OfType<ToolTip>())
                {
                  tooltip.DataContext = tooltip.Content is StackPanel panel && panel.GetBindingExpression(FrameworkElement.DataContextProperty) != null
                    ? model : model.CurrentStatus;
                  CheckPopup(tooltip, target, app, "ReadTestStructureReturnsSampleStructureOfAllTestDataItemsAsync");
                  var caption = Descendants<TextBlock>(tooltip).FirstOrDefault(text => text.Text == Strings.ContributorsCaption);
                  Assert.That(caption, Is.Not.Null, "Contributor captions follow the interface language in both plugins.");
                  if (plugin.ConnectorType == "Jenkins")
                  {
                    Assert.That(Descendants<TextBlock>(tooltip).Any(text => text.IsVisible && text.Text == Strings.CommittersCaption), Is.True);
                  }
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
              status.Update(new Status { State = ObservationState.Running, Building = true, Time = DateTime.UtcNow.AddMinutes(-minutes), Duration = TimeSpan.FromMinutes(minutes), EstimatedDuration = TimeSpan.FromMinutes(8) });
              var running = new StackPanel { DataContext = status };
              running.Children.Add(new HistoryBuildTooltipControl());
              running.Children.Add(new ProgressBarTooltipControl());
              CheckPopup(new ToolTip { Content = running }, target, app);
              Assert.That(Descendants<TextBlock>(running).Any(text => text.IsVisible && text.Name == "FinishedTimeText"), Is.False);
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

    private static void CheckPopup(ToolTip tooltip, UIElement target, App app, string? expectedText = null)
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
        if (expectedText != null)
        {
          Assert.That(text.Any(block => block.Text.Replace("\u200B", string.Empty).Contains(expectedText, StringComparison.Ordinal)), Is.True, "Failure summaries are visible in row, build, and history tooltips.");
        }

        Assert.That(text.All(block => block.Foreground == tooltip.Foreground || block.Foreground == app.Resources["SettingsMuted"]
          || block.Foreground == app.Resources["TooltipError"] || block.Foreground == app.Resources["TooltipWarning"]), Is.True);
        Assert.That(text.Any(block => block.Text.StartsWith("Build contributor ", StringComparison.Ordinal)), Is.False);
        Assert.That(tooltip.ActualHeight, Is.LessThan(450), "Contributor lists must not make the compact tooltip grow.");
        if (expectedText != null)
        {
          var failure = text.First(block => block.Text.Replace("\u200B", string.Empty).Contains(expectedText, StringComparison.Ordinal));
          var contributors = text.First(block => block.Text == Strings.ContributorsCaption);
          Assert.That(failure.TranslatePoint(default, tooltip).Y, Is.GreaterThan(contributors.TranslatePoint(default, tooltip).Y));
          Assert.That(text.Any(block => block.Text == "36"), Is.True);
          var warning = text.First(block => block.Text == Strings.SkippedTestsNotice);
          Assert.That(warning.TranslatePoint(default, tooltip).Y, Is.GreaterThan(failure.TranslatePoint(default, tooltip).Y));
          Assert.That(failure.FontFamily.Source, Does.Contain("Cascadia Code"));
          Assert.That(failure.Foreground, Is.SameAs(app.Resources["SettingsMuted"]));
          var footer = Descendants<Border>(tooltip).Single(border => border.Background == app.Resources["TooltipFailureSurface"]);
          Assert.That(footer.TranslatePoint(default, tooltip).X, Is.EqualTo(1).Within(1), "The tinted footer spans the popup width.");
          Assert.That(footer.ActualWidth, Is.EqualTo(tooltip.ActualWidth - 2).Within(1));
        }

        var lastTextBottom = text.Max(block => block.TranslatePoint(new Point(0, block.ActualHeight), tooltip).Y);
        Assert.That(tooltip.ActualHeight - lastTextBottom, Is.InRange(15, 21), "Tooltip content must not end with blank spacer rows.");
        if (expectedText != null && CultureInfo.CurrentUICulture.Name == "de-DE" && text.Any(block => block.Text == Strings.CommittersCaption))
        {
          var bitmap = new RenderTargetBitmap((int)Math.Ceiling(tooltip.ActualWidth), (int)Math.Ceiling(tooltip.ActualHeight), 96, 96, PixelFormats.Pbgra32);
          bitmap.Render(tooltip);
          var encoder = new PngBitmapEncoder();
          encoder.Frames.Add(BitmapFrame.Create(bitmap));
          var folder = Path.Combine(AppContext.BaseDirectory, "TestResults");
          Directory.CreateDirectory(folder);
          var suffix = ((SolidColorBrush)tooltip.Background).Color.R < 100 ? "dark" : "light";
          using var stream = File.Create(Path.Combine(folder, "compact-tooltip-" + suffix + ".png"));
          encoder.Save(stream);
        }

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
