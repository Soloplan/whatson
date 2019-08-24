// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PluginFinder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Composition
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Reflection;
  using NLog;

  public static class PluginFinder
  {
    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// Finds all the <see cref="T:Soloplan.WhatsON.Composition.IPlugin"/>s that are provided by the specified assemblies.
    /// Note that this method only supports assemblies from the applications root directory,
    /// i.e. the plugin assembly must be located next to the application's executable
    /// </summary>
    /// <param name="assemblies">A list of assemblies that contain plugins.</param>
    /// <returns>An enumerator with all the found plugins.</returns>
    public static IEnumerable<IPlugin> FindAllPlugins(params string[] assemblies)
    {
      foreach (var assemblyName in assemblies)
      {
        var absoluteName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName);
        if (!File.Exists(absoluteName))
        {
          log.Warn("Assembly {absoluteName} doesn't exist.", absoluteName);
          continue;
        }

        log.Debug("Scanning assembly {absoluteName} for plugins.", absoluteName);
        var assembly = Assembly.LoadFile(absoluteName);
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
  }
}
