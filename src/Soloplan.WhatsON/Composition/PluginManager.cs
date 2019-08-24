// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlugInsManager.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Composition
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using NLog;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// The Manager for Connector Plugins.
  /// </summary>
  public sealed class PluginManager
  {
    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// Singleton instance.
    /// </summary>
    private static volatile PluginManager instance;

    /// <summary>
    /// The connectors.
    /// </summary>
    private readonly IList<Connector> connectors = new List<Connector>();

    /// <summary>
    /// The connector plugins list.
    /// </summary>
    private List<IConnectorPlugin> connectorPlugins;

    /// <summary>
    /// Registered plugins.
    /// </summary>
    private List<IPlugin> plugIns = new List<IPlugin>();

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginManager"/> class.
    /// </summary>
    private PluginManager()
    {
      this.InitializePlugInTypes();
    }

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    /// <remarks>
    /// Getter of this property is thread-safe.
    /// </remarks>
    public static PluginManager Instance
    {
      [MethodImpl(MethodImplOptions.Synchronized)]
      get => instance ?? (instance = new PluginManager());
    }

    /// <summary>
    /// Gets the read only list of Connector Plugins.
    /// </summary>
    public IReadOnlyList<IConnectorPlugin> ConnectorPlugins
    {
      get
      {
        if (this.connectorPlugins != null)
        {
          return this.connectorPlugins.AsReadOnly();
        }

        this.connectorPlugins = new List<IConnectorPlugin>();
        foreach (var plugIn in this.plugIns.OfType<IConnectorPlugin>())
        {
          if (plugIn.ConnectorType == null)
          {
            continue;
          }

          this.connectorPlugins.Add(plugIn);
          log.Info($"Loaded connector plugin {plugIn}.");
        }

        return this.connectorPlugins.AsReadOnly();
      }
    }

    public IReadOnlyList<IPlugin> PlugIns => this.plugIns.AsReadOnly();

    /// <summary>
    /// Gets the Plugin instance of a Connector Plugin.
    /// </summary>
    /// <param name="connector">The connector.</param>
    /// <returns>The Plugin instance.</returns>
    public IConnectorPlugin GetPlugin(Connector connector)
    {
      return this.ConnectorPlugins.FirstOrDefault(sp => sp.ConnectorType == connector.GetType());
    }

    /// <summary>
    /// Gets the Plugin instance of a Connector Configuration.
    /// </summary>
    /// <param name="connectorConfiguration">The connector configuration.</param>
    /// <returns>The Plugin instance.</returns>
    public IConnectorPlugin GetPlugin(ConnectorConfiguration connectorConfiguration)
    {
      return this.ConnectorPlugins.FirstOrDefault(sp => sp.GetType().FullName == connectorConfiguration.PluginTypeName);
    }

    /// <summary>
    /// Creates the new connector.
    /// </summary>
    /// <param name="connectorConfiguration">The connector configuration.</param>
    /// <returns>Creates new connector with given configuration.</returns>
    /// <exception cref="InvalidOperationException">Couldn't find plugin for a type: {connectorConfiguration.TypeName}.</exception>
    public Connector CreateNewConnector(ConnectorConfiguration connectorConfiguration)
    {
      var plugin = this.ConnectorPlugins.FirstOrDefault(p => p.GetType().FullName == connectorConfiguration.PluginTypeName);
      if (plugin == null)
      {
        log.Error("Couldn't find plugin for a type: {pluginType}", connectorConfiguration.PluginTypeName);
        return null;
      }

      var connector = plugin.CreateNew(connectorConfiguration);
      this.connectors.Add(connector);
      return connector;
    }

    /// <summary>
    /// Gets the connector.
    /// </summary>
    /// <param name="connectorConfiguration">The connector configuration.</param>
    /// <returns>A new connector with given configuration.</returns>
    /// <exception cref="InvalidOperationException">Couldn't find plugin for a type: {connectorConfiguration.TypeName}.</exception>
    public Connector GetConnector(ConnectorConfiguration connectorConfiguration)
    {
      var connector = this.connectors.FirstOrDefault(s => s.ConnectorConfiguration.Identifier == connectorConfiguration.Identifier);
      if (connector != null)
      {
        return connector;
      }

      return this.CreateNewConnector(connectorConfiguration);
    }

    /// <summary>
    /// Initializes the Plugin types.
    /// </summary>
    private void InitializePlugInTypes()
    {
      log.Debug("Initializing {PluginsManager}", nameof(PluginManager));
      var path = System.IO.Path.GetDirectoryName(System.AppContext.BaseDirectory);
      var plugInPath = Path.Combine(path, "Plugins");
      log.Debug("Paths used {}", new { AppDirectory = path, PluginDirectory = plugInPath });
      if (Directory.Exists(plugInPath))
      {
        var found = PluginFinder.FindAllPlugins(Directory.EnumerateFiles(plugInPath, "*.dll").ToArray());
        this.plugIns = new List<IPlugin>();
        foreach (var plugin in found)
        {
          this.plugIns.Add(plugin);
        }
      }
      else
      {
        log.Warn("Plugins directory not found");
      }

      log.Debug("PluginsManager initialized.");
    }
  }
}