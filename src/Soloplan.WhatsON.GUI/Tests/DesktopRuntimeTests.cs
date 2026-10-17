// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DesktopRuntimeTests.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Markup;
  using System.Windows.Threading;
  using NLog;
  using NLog.Config;
  using NUnit.Framework;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Configuration;
  using Soloplan.WhatsON.GUI.Configuration.View;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [NonParallelizable]
  public class DesktopRuntimeTests
  {
    [Test]
    [Apartment(ApartmentState.STA)]
    public void ApplicationResourcesAndBuiltInPluginsLoadOnDesktopRuntime()
    {
      // WPF queues startup even without Run; keep rendering isolated from real servers.
      var app = new ResourceTestApp { ShutdownMode = ShutdownMode.OnExplicitShutdown };
      app.Resources = (ResourceDictionary)Application.LoadComponent(new Uri("/Soloplan.WhatsON.GUI;component/ApplicationResources.xaml", UriKind.Relative));
      app.ApplyTheme(ApplicationTheme.Light);
      Assert.That(app.IsDarkThemeEnabled, Is.False);
      app.ApplyTheme(ApplicationTheme.Dark);
      Assert.That(app.IsDarkThemeEnabled, Is.True);
      Assert.That(app.Resources["MaterialDesignPaper"], Is.Not.Null);

      var plugins = PluginManager.Instance.Plugins;
      Assert.That(
        plugins.OfType<ConnectorPlugin>().Select(plugin => plugin.Name),
        Is.EquivalentTo(new[] { "Jenkins", "CruiseControl" }));
      var presentationPlugins = plugins.OfType<PresentationPlugin>().ToList();
      Assert.That(presentationPlugins, Has.Count.EqualTo(2));
      foreach (var plugin in presentationPlugins)
      {
        using (var reader = plugin.GetDataTempletXaml())
        {
          var resources = (ResourceDictionary)XamlReader.Load(reader);
          var templates = resources.Values.OfType<DataTemplate>().ToList();
          Assert.That(templates, Is.Not.Empty, plugin.ConnectorType);
          foreach (var template in templates)
          {
            Assert.That(template.LoadContent(), Is.Not.Null, plugin.ConnectorType);
          }
        }
      }

      var scheduler = new ObservationScheduler();
      var configuration = new ApplicationConfiguration();
      var connectors = new List<Connector>();
      var window = new MainWindow(scheduler, configuration, connectors);
      try
      {
        ((ConnectorsTreeView)window.FindName("mainTreeView")).Init(scheduler, configuration, connectors);
        Assert.That(window.Content, Is.Not.Null);
        Assert.That(window.Style, Is.Not.Null);
      }
      finally
      {
        window.Close();
      }

      var preferences = new ApplicationConfiguration { ThemeMode = ApplicationTheme.Light };
      var settings = new ConfigWindow(preferences, new WindowSettings());
      try
      {
        var sections = (ListBox)settings.FindName("ConfigTopicsListBox");
        var frame = (Frame)settings.FindName("ConfigFrame");
        foreach (var mode in new[] { ApplicationTheme.Light, ApplicationTheme.Dark })
        {
          app.ApplyTheme(mode);
          foreach (var section in new[] { 0, 1, 2 })
          {
            sections.SelectedIndex = section;
            settings.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Assert.That(frame.Content, Is.InstanceOf<Page>());
            var page = (Page)frame.Content;
            page.Measure(new Size(700, 650));
            page.Arrange(new Rect(0, 0, 700, 650));
            page.UpdateLayout();
            Assert.That(page.ActualWidth, Is.EqualTo(700));
            if (page is ConnectorsPage)
            {
              Assert.That(((Border)page.FindName("EmptyProjectsState")).Visibility, Is.EqualTo(Visibility.Visible));
              Assert.That(((Button)page.FindName("DeleteButton")).IsEnabled, Is.False);
            }
          }
        }

        sections.SelectedIndex = 0;
        settings.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        var selector = (ComboBox)((Page)frame.Content).FindName("ThemeModeSelector");
        selector.SelectedValue = ApplicationTheme.Dark;
        Assert.That(app.IsDarkThemeEnabled, Is.True);
        Assert.That(settings.ConfigurationViewModel.ThemeMode, Is.EqualTo(ApplicationTheme.Dark));
        Assert.That(preferences.ThemeMode, Is.EqualTo(ApplicationTheme.Light));

        ((Button)settings.FindName("ResetButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.That(app.IsDarkThemeEnabled, Is.False);
        Assert.That(selector.SelectedValue, Is.EqualTo(ApplicationTheme.Light));
        selector.SelectedValue = ApplicationTheme.System;
        Assert.That(app.CurrentThemeMode, Is.EqualTo(ApplicationTheme.System));
        Assert.That(app.IsDarkThemeEnabled, Is.EqualTo(ThemeHelper.SystemUsesDarkMode()));
        selector.SelectedValue = ApplicationTheme.Dark;
      }
      finally
      {
        settings.Close();
      }

      Assert.That(app.IsDarkThemeEnabled, Is.False, "Closing without saving must restore the saved appearance.");

      var projectSettings = new ApplicationConfiguration { ThemeMode = ApplicationTheme.Light };
      projectSettings.ConnectorsConfiguration.Add(new ConnectorConfiguration("Jenkins", "Application build", new List<ConfigurationItem>
      {
        new ConfigurationItem(Connector.ServerAddress) { Value = "https://ci.example.invalid" },
        new ConfigurationItem(Connector.ProjectName) { Value = "main" },
      }));
      var populatedSettings = new ConfigWindow(projectSettings, new WindowSettings());
      try
      {
        ((ListBox)populatedSettings.FindName("ConfigTopicsListBox")).SelectedIndex = 1;
        populatedSettings.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        var page = (Page)((Frame)populatedSettings.FindName("ConfigFrame")).Content;
        page.Measure(new Size(550, 450));
        page.Arrange(new Rect(0, 0, 550, 450));
        page.UpdateLayout();
        Assert.That(((ComboBox)page.FindName("uxConnectors")).SelectedItem, Is.Not.Null);
        Assert.That(((Border)page.FindName("EmptyProjectsState")).Visibility, Is.EqualTo(Visibility.Collapsed));
        Assert.That(((Button)page.FindName("DeleteButton")).IsEnabled, Is.True);
        Assert.That(((Frame)page.FindName("uxConnectorFrame")).Content, Is.InstanceOf<ConnectorConfigPage>());
      }
      finally
      {
        populatedSettings.Close();
      }

      using (var stream = Application.GetResourceStream(new Uri("/Soloplan.WhatsON.GUI;component/Logging/LogConfig.xml", UriKind.Relative)).Stream)
      using (var reader = new StreamReader(stream))
      using (var factory = new LogFactory())
      {
        var loggingConfiguration = XmlLoggingConfiguration.CreateFromXmlString(reader.ReadToEnd(), factory);
        Assert.That(
          loggingConfiguration.AllTargets.Select(target => target.Name),
          Is.EquivalentTo(new[] { "whatsONMainLog", "whatsONConsoleLog" }));
      }
    }

    private sealed class ResourceTestApp : App
    {
      protected override void OnStartup(StartupEventArgs e)
      {
      }
    }
  }
}
