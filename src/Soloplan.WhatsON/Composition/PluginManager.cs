// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PluginManager.cs" company="Soloplan GmbH">
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
  using System.Reflection;
  using System.Runtime.CompilerServices;
  using NLog;
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
    private List<ConnectorPlugin> connectorPlugins;

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
    public IReadOnlyList<ConnectorPlugin> ConnectorPlugins
    {
      get
      {
        if (this.connectorPlugins != null)
        {
          return this.connectorPlugins.AsReadOnly();
        }

        this.connectorPlugins = new List<ConnectorPlugin>();
        foreach (var plugIn in this.plugIns.OfType<ConnectorPlugin>())
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

    public IReadOnlyList<IPlugin> Plugins => this.plugIns.AsReadOnly();

    /// <summary>
    /// Gets the Plugin instance of a Connector Plugin.
    /// </summary>
    /// <param name="connector">The connector.</param>
    /// <returns>The Plugin instance.</returns>
    public ConnectorPlugin GetPlugin(Connector connector)
    {
      return this.ConnectorPlugins.FirstOrDefault(sp => sp.ConnectorType == connector.GetType());
    }

    /// <summary>
    /// Gets the Plugin instance of a Connector Configuration.
    /// </summary>
    /// <param name="connectorConfiguration">The connector configuration.</param>
    /// <returns>The Plugin instance.</returns>
    public ConnectorPlugin GetPlugin(ConnectorConfiguration connectorConfiguration)
    {
      return this.ConnectorPlugins.FirstOrDefault(sp => sp.Name == connectorConfiguration.Type);
    }

    /// <summary>
    /// Creates the new connector.
    /// </summary>
    /// <param name="connectorConfiguration">The connector configuration.</param>
    /// <returns>Creates new connector with given configuration.</returns>
    /// <exception cref="InvalidOperationException">Couldn't find plugin for a type: {connectorConfiguration.TypeName}.</exception>
    public Connector CreateNewConnector(ConnectorConfiguration connectorConfiguration, bool? checkRedirect = null)
    {
      var plugin = this.ConnectorPlugins.FirstOrDefault(p => p.Name == connectorConfiguration.Type);
      if (plugin == null)
      {
        log.Error("Couldn't find plugin for a type: {pluginType}", connectorConfiguration.Type);
        return null;
      }

      Connector newConnector;
      try
      {
        newConnector = plugin.CreateNew(connectorConfiguration, checkRedirect);
      }
      catch (Exception e)
      {
        log.Error(e);
        return null;
      }

      this.connectors.Add(newConnector);
      return newConnector;
    }

    /// <summary>
    /// Gets the connector.
    /// </summary>
    /// <param name="connectorConfiguration">The connector configuration.</param>
    /// <returns>A new connector with given configuration.</returns>
    /// <exception cref="InvalidOperationException">Couldn't find plugin for a type: {connectorConfiguration.TypeName}.</exception>
    public Connector GetConnector(ConnectorConfiguration connectorConfiguration, bool? checkRedirect = null)
    {
      var connector = this.connectors.FirstOrDefault(s => s != null && s.Configuration.Identifier == connectorConfiguration.Identifier);
      if (connector != null)
      {
        return connector;
      }

      return this.CreateNewConnector(connectorConfiguration, checkRedirect);
    }

    /// <summary>
    /// Finds all the <see cref="T:Soloplan.WhatsON.Composition.IPlugin"/>s that are provided by the specified assemblies.
    /// Note that this method only supports assemblies from the applications root directory,
    /// i.e. the plugin assembly must be located next to the application's executable.
    /// </summary>
    /// <param name="assemblies">A list of assemblies that contain plugins.</param>
    /// <returns>An enumerator with all the found plugins.</returns>
    private static IEnumerable<IPlugin> FindAllPlugins(params string[] assemblies)
    {
      foreach (var assemblyName in assemblies)
      {
        if(!assemblyName.Contains("WhatsON"))
        {
          continue;
        }

        var absoluteName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName);
        if (!File.Exists(absoluteName))
        {
          log.Warn("Assembly {absoluteName} doesn't exist.", absoluteName);
          continue;
        }

        log.Debug("Scanning assembly {absoluteName} for plugins.", absoluteName);
        Assembly assembly;
        try
        {
          assembly = Assembly.LoadFrom(absoluteName);
        }
        catch (FileLoadException loadEx)
        {
          log.Error($"Failed to load file {assemblyName}. Check if the plugIn isn't blocked.");
          log.Error(loadEx);
          continue;
        }
        catch (Exception e)
        {
          log.Error(e, "Failed to load assembly from PlugIn directory");
          continue;
        }

        foreach (var type in assembly.GetExportedTypes())
        {
          if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
          {
            log.Debug("Found plugin {plugin}", new { Assembly = absoluteName, PluginType = type });
            yield return Activator.CreateInstance(type) as IPlugin;
          }
        }

        log.Debug("Assembly {absoluteName} scanned for plugins.", absoluteName);
      }
    }

    /// <summary>
    /// Initializes the Plugin types.
    /// </summary>
    private void InitializePlugInTypes()
    {
      log.Debug("Initializing {PluginsManager}", nameof(PluginManager));
      var path = System.IO.Path.GetDirectoryName(System.AppContext.BaseDirectory);
      var plugInPath = path;
      log.Debug("Paths used {}", new { AppDirectory = path, PluginDirectory = plugInPath });
      if (Directory.Exists(plugInPath))
      {
        var found = FindAllPlugins(Directory.EnumerateFiles(plugInPath, "*.dll").ToArray());
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