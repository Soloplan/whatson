// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationHelper.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Serialization
{
  using System;
  using System.IO;
  using Newtonsoft.Json;

  /// <summary>
  /// The serializer which is responsible for the for loading, saving or creating empty configuration.
  /// </summary>
  public static class SerializationHelper
  {
    public static readonly string ConfigFileExtension = "json";
    public static readonly string ConfigFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\WhatsOn";
    public static readonly string ConfigFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\WhatsOn\\configuration." + ConfigFileExtension;

    /// <summary>
    /// Saves the configuration to specified file.
    /// </summary>
    /// <typeparam name="T">The type of configuration.</typeparam>
    /// <param name="configuration">The configuration.</param>
    /// <param name="file">The file path.</param>
    public static void Save<T>(T configuration, string file)
    {
      var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, NullValueHandling = NullValueHandling.Ignore,  Formatting = Formatting.Indented };
      var json = JsonConvert.SerializeObject(configuration, settings);
      File.WriteAllText(file, json);
    }

    public static T Load<T>(string file)
    {
      var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
      return JsonConvert.DeserializeObject<T>(File.ReadAllText(file), settings);
    }

    public static ApplicationConfiguration LoadConfiguration()
    {
      return Load<ApplicationConfiguration>(ConfigFile);
    }

    /// <summary>
    /// Loads the or creates the configuration.
    /// </summary>
    /// <returns>The existing or new configuration.</returns>
    public static ApplicationConfiguration LoadOrCreateConfiguration()
    {
      if (File.Exists(ConfigFile))
      {
        return LoadConfiguration();
      }

      return new ApplicationConfiguration();
    }

    /// <summary>
    /// Saves the configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public static void SaveConfiguration(ApplicationConfiguration configuration)
    {
      var configFilePath = Path.GetDirectoryName(ConfigFile);
      if (configFilePath == null)
      {
        throw new InvalidOperationException("Couldn't get the directory for configuration file.");
      }

      Directory.CreateDirectory(configFilePath);
      Save(configuration, ConfigFile);
    }
  }
}