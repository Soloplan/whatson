// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigResourcesHelper.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Reflection;
  using System.Resources;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

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
    /// <param name="subjectType">Type of the subject.</param>
    public static void ApplyConfigResourses(ConfigurationItemAttribute configurationItemAttribute, Type subjectType)
    {
      ApplyCaptionConfigResources(configurationItemAttribute, subjectType);
    }

    /// <summary>
    /// Applies the caption configuration resources.
    /// </summary>
    /// <param name="configurationItemAttribute">The configuration item attribute.</param>
    /// <param name="subjectType">Type of the subject.</param>
    private static void ApplyCaptionConfigResources(ConfigurationItemAttribute configurationItemAttribute, Type subjectType)
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
      var currentAsemblyResourceResult = GetConfigResourceByName(configurationItemAttribute.Key, typeof(SubjectViewModel).Assembly);
      if (currentAsemblyResourceResult != null)
      {
        captionsCache[configurationItemAttribute.Key] = currentAsemblyResourceResult;
        configurationItemAttribute.Caption = currentAsemblyResourceResult;
        return;
      }

      scannedAssembliesCache.Add(typeof(SubjectViewModel).Assembly);

      // search in current subject assembly
      var currentSubjectAssembly = subjectType.Assembly;
      if (!scannedAssembliesCache.Contains(currentSubjectAssembly))
      {
        var subjecttAsemblyResourceResult = GetConfigResourceByName(configurationItemAttribute.Key, currentSubjectAssembly);
        if (subjecttAsemblyResourceResult != null)
        {
          configurationItemAttribute.Caption = subjecttAsemblyResourceResult;
          captionsCache[configurationItemAttribute.Key] = subjecttAsemblyResourceResult;
          return;
        }
      }

      // search parent assemblies
      var parentTypes = subjectType.GetParentTypes();
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