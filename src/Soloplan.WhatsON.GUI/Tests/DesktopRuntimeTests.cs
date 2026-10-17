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
  using System.Windows.Markup;
  using NLog;
  using NLog.Config;
  using NUnit.Framework;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  [TestFixture]
  [NonParallelizable]
  public class DesktopRuntimeTests
  {
    [Test]
    [Apartment(ApartmentState.STA)]
    public void ApplicationResourcesAndBuiltInPluginsLoadOnDesktopRuntime()
    {
      // Do not run startup: it loads the user's configuration and starts polling.
      var app = new App();
      app.InitializeComponent();
      app.ApplyTheme(false);
      app.ApplyTheme(true);
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
  }
}
