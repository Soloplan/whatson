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
    public static readonly string ConfigFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\WhatsOn\\configuration.json";

    public static void Save<T>(T subject, string file)
    {
      var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, NullValueHandling = NullValueHandling.Ignore,  Formatting = Formatting.Indented };
      var json = JsonConvert.SerializeObject(subject, settings);
      File.WriteAllText(file, json);
    }

    public static T Load<T>(string file)
    {
      var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
      return JsonConvert.DeserializeObject<T>(File.ReadAllText(file), settings);
    }

    public static Configuration LoadConfiguration()
    {
      return Load<Configuration>(ConfigFile);
    }

    /// <summary>
    /// Loads the or creates the configuration.
    /// </summary>
    /// <returns>The existing or new configuration.</returns>
    public static Configuration LoadOrCreateConfiguration()
    {
      if (File.Exists(ConfigFile))
      {
        return LoadConfiguration();
      }

      return new Configuration();
    }

    /// <summary>
    /// Saves the configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public static void SaveConfiguration(Configuration configuration)
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