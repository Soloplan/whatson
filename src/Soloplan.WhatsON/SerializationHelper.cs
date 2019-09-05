// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationHelper.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System;
  using System.IO;
  using System.Net;
  using System.Threading;
  using System.Threading.Tasks;
  using Newtonsoft.Json;
  using NLog;
  using Soloplan.WhatsON.Configuration;

  /// <summary>
  /// The serializer which is responsible for the for loading, saving or creating empty configuration.
  /// </summary>
  public static class SerializationHelper
  {
    public static readonly string ConfigFileExtension = "json";

    public static readonly string ConfigFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\WhatsOn";

    public static readonly string ConfigFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\WhatsOn\\configuration." + ConfigFileExtension;

    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// Saves the configuration to specified file.
    /// </summary>
    /// <typeparam name="T">The type of configuration.</typeparam>
    /// <param name="configuration">The configuration.</param>
    /// <param name="file">The file path.</param>
    public static void Save<T>(T configuration, string file)
    {
      log.Debug("Saving configuration {configuration} to file {file}.", configuration, file);
      var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented };
      var json = JsonConvert.SerializeObject(configuration, settings);
      File.WriteAllText(file, json);
      log.Debug("Configuration saved.");
    }

    public static T Load<T>(string file)
    {
      var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
      return JsonConvert.DeserializeObject<T>(File.ReadAllText(file), settings);
    }

    public static ApplicationConfiguration LoadConfiguration()
    {
      log.Debug("Loading configuration form file {file}", ConfigFile);
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

      log.Debug("Configuration file {file} doesn't exist. Create new default configuration.", ConfigFile);
      return new ApplicationConfiguration();
    }

    /// <summary>
    /// Saves the configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public static void SaveConfiguration(ApplicationConfiguration configuration, string configFilePath = null)
    {
      if (configFilePath == null)
      {
        var configFileDirecory = Path.GetDirectoryName(ConfigFile);
        if (configFileDirecory == null)
        {
          throw new InvalidOperationException("Couldn't get the directory for configuration file.");
        }

        Directory.CreateDirectory(configFileDirecory);
      }

      Save(configuration, configFilePath ?? ConfigFile);
    }

    public static async Task<TModel> GetJsonModel<TModel>(string requestUrl, CancellationToken token = default, Action<WebRequest> requestCallback = null)
     where TModel : class
    {
      const string JSON_CONTENT_TYPE = "application/json";

      var request = (HttpWebRequest)WebRequest.Create(requestUrl);
      request.Accept = JSON_CONTENT_TYPE;
      request.ContentType = JSON_CONTENT_TYPE;
      requestCallback?.Invoke(request);

      try
      {
        log.Trace($"Fetching JSON object ({typeof(TModel)}) from \"{requestUrl}\" ...");
        using (token.Register(() => request.Abort(), false))
        using (var response = (HttpWebResponse)await request.GetResponseAsync())
        {
          if (response.StatusCode != HttpStatusCode.OK)
          {
            throw new InvalidPlugInApiResponseException($"Error while accessing the API via \"{requestUrl}\": The server returned the error code: {response.StatusCode}: {response.StatusDescription}. ");
          }

          if (!response.ContentType.Contains(JSON_CONTENT_TYPE))
          {
            throw new InvalidPlugInApiResponseException($"The API didn't return JSON content from the requested url \"{requestUrl}\". Content type was \"{response.ContentType}\".");
          }

          // Get the stream containing content returned by the server
          // Open the stream using a StreamReader for easy access
          using (var dataStream = response.GetResponseStream())
          using (var reader = new StreamReader(dataStream))
          {
            var responseFromServer = reader.ReadToEnd();
            var settings = new JsonSerializerSettings
            {
              Error = (s, e) =>
              {
                e.ErrorContext.Handled = true;
                throw new InvalidPlugInApiResponseException($"Error during model deserialization for type {typeof(TModel).Name}");
              },
            };

            return JsonConvert.DeserializeObject<TModel>(responseFromServer, settings);
          }
        }
      }
      catch (WebException ex)
      {
        if (token.IsCancellationRequested)
        {
          throw new OperationCanceledException(ex.Message, ex, token);
        }

        throw;
      }
    }
  }
}