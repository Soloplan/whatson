// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigResourcesHelper.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Reflection;
  using System.Resources;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;

  /// <summary>
  /// Helps to get resources for configuration.
  /// </summary>
  public static class ConfigResourcesHelper
  {
    /// <summary>
    /// The file name where resources for the configuration items are kept.
    /// </summary>
    private const string ConfigurationItemsFileName = "ConfigurationItems";

    /// <summary>
    /// The cache for captions. The key is the resource tag.
    /// </summary>
    private static Dictionary<string, string> captionsCache;

    /// <summary>
    /// Applies the configuration resources.
    /// </summary>
    /// <param name="configurationItemAttribute">The configuration item attribute.</param>
    /// <param name="connectorType">Type of the connector.</param>
    public static void ApplyConfigResources(ConfigurationItemAttribute configurationItemAttribute, Type connectorType)
    {
      ApplyCaptionConfigResources(configurationItemAttribute, connectorType);
    }

    /// <summary>
    /// Applies the caption configuration resources.
    /// </summary>
    /// <param name="configurationItemAttribute">The configuration item attribute.</param>
    /// <param name="connectorType">Type of the connector.</param>
    private static void ApplyCaptionConfigResources(ConfigurationItemAttribute configurationItemAttribute, Type connectorType)
    {
      if (captionsCache == null)
      {
        captionsCache = new Dictionary<string, string>();
      }

      if (captionsCache.ContainsKey(configurationItemAttribute.Key))
      {
        configurationItemAttribute.Caption = captionsCache[configurationItemAttribute.Key];
        return;
      }

      IList<Assembly> scannedAssembliesCache = new List<Assembly>();

      // firstly search in current assembly
      var currentAsemblyResourceResult = GetConfigResourceByName(configurationItemAttribute.Key, typeof(ConnectorViewModel).Assembly);
      if (currentAsemblyResourceResult != null)
      {
        captionsCache[configurationItemAttribute.Key] = currentAsemblyResourceResult;
        configurationItemAttribute.Caption = currentAsemblyResourceResult;
        return;
      }

      scannedAssembliesCache.Add(typeof(ConnectorViewModel).Assembly);

      // search in current connector assembly
      var currentConnectorAssembly = connectorType.Assembly;
      if (!scannedAssembliesCache.Contains(currentConnectorAssembly))
      {
        var connectorsAssemblyResourceResult = GetConfigResourceByName(configurationItemAttribute.Key, currentConnectorAssembly);
        if (connectorsAssemblyResourceResult != null)
        {
          configurationItemAttribute.Caption = connectorsAssemblyResourceResult;
          captionsCache[configurationItemAttribute.Key] = connectorsAssemblyResourceResult;
          return;
        }
      }

      // search parent assemblies
      var parentTypes = connectorType.GetParentTypes();
      foreach (var parentType in parentTypes)
      {
        var parentTypeAssembly = parentType.Assembly;
        if (!scannedAssembliesCache.Contains(parentTypeAssembly))
        {
          var parentTypeAsemblyResourceResult = GetConfigResourceByName(configurationItemAttribute.Key, parentTypeAssembly);
          if (parentTypeAsemblyResourceResult != null)
          {
            configurationItemAttribute.Caption = parentTypeAsemblyResourceResult;
            captionsCache[configurationItemAttribute.Key] = parentTypeAsemblyResourceResult;
            return;
          }
        }
      }
    }

    /// <summary>
    /// Gets the name of the configuration resource by.
    /// </summary>
    /// <param name="resourceName">Name of the resource.</param>
    /// <param name="assemblyForResourceDiscovery">The assembly for resource discovery.</param>
    /// <returns>The resource from the configuration resource file.</returns>
    private static string GetConfigResourceByName(string resourceName, Assembly assemblyForResourceDiscovery)
    {
      using (var resourceStream = assemblyForResourceDiscovery.GetManifestResourceStream($"{assemblyForResourceDiscovery.GetName().Name}.Properties.{ConfigurationItemsFileName}.resources"))
      {
        if (resourceStream == null)
        {
          return null;
        }

        using (var resXReader = new ResourceReader(resourceStream))
        {
          foreach (DictionaryEntry resEntry in resXReader)
          {
            if ((string)resEntry.Key == resourceName)
            {
              return (string)resEntry.Value;
            }
          }
        }
      }

      return null;
    }
  }
}