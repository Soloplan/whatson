// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationHelper.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Net;
  using System.Threading;
  using System.Threading.Tasks;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Serialization;
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
    public static void Save<T>(T configuration, string file, bool serializeIdentifier = true)
    {
      log.Debug("Saving configuration {configuration} to file {file}.", configuration, file);
      var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented };
      if (!serializeIdentifier)
      {
        settings.ContractResolver = new WhatsOnContractResolver(nameof(ConnectorConfiguration.Identifier));
      }
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

    public static async Task<TModel> GetJsonModel<TModel>(string requestUrl, CancellationToken token = default(CancellationToken), Action<WebRequest> requestCallback = null)
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

    /// <summary>
    /// The contract resolver capable of ignoring certain properties.
    /// </summary>
    /// <seealso cref="Newtonsoft.Json.Serialization.DefaultContractResolver" />
    private class WhatsOnContractResolver : DefaultContractResolver
    {
      /// <summary>
      /// The properties.
      /// </summary>
      private readonly string[] properties;

      /// <summary>
      /// Initializes a new instance of the <see cref="WhatsOnContractResolver"/> class.
      /// </summary>
      /// <param name="properties">The properties.</param>
      public WhatsOnContractResolver(params string[] properties)
      {
        this.properties = properties;
      }

      /// <summary>
      /// Creates properties for the given <see cref="T:Newtonsoft.Json.Serialization.JsonContract" />.
      /// </summary>
      /// <param name="type">The type to create properties for.</param>
      /// <param name="memberSerialization">The member serialization mode for the type.</param>
      /// <returns>
      /// Properties for the given <see cref="T:Newtonsoft.Json.Serialization.JsonContract" />.
      /// </returns>
      protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
      {
        var resultProperties = base.CreateProperties(type, memberSerialization);
        resultProperties = resultProperties.Where(p => !this.properties.Contains(p.PropertyName)).ToList();
        return resultProperties;
      }
    }
  }
}