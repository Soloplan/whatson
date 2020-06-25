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
  using System.Diagnostics;
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
  public sealed class SerializationHelper
  {
    /// <summary>
    /// The  <see cref="Lazy{T}"/> instance for the singleton.
    /// </summary>
    static readonly Lazy<SerializationHelper> lazy = new Lazy<SerializationHelper>(() => new SerializationHelper());

    /// <summary>
    /// The instance of the <see cref="SerializationHelper"/>.
    /// </summary>
    public static SerializationHelper Instance => lazy.Value;

    /// <summary>
    /// The configuration file extension.
    /// </summary>
    public readonly string ConfigFileExtension = "json";

    /// <summary>
    /// The configuration folder path, without the back slash.
    /// </summary>
    public string ConfigFolder;

    public string ConfigFile => this.ConfigFolder + "\\configuration." + this.ConfigFileExtension;

    public SerializationHelper()
    {
      this.ConfigFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\WhatsOn";
    }

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
    /// <param name="serializeIdentifier">if set to <c>true</c> serialize the identifier.</param>
    public void Save<T>(T configuration, string file, bool serializeIdentifier = true)
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

    public ApplicationConfiguration LoadConfiguration()
    {
      log.Debug("Loading configuration form file {file}", ConfigFile);
      var result = Load<ApplicationConfiguration>(ConfigFile);
      this.HandleConfigurationErrors(result);
      return result;
    }

    /// <summary>
    /// Sets the configuration folder to given path.
    /// </summary>
    /// <param name="configDir">The new configuration Directory Path. Can be null, then default is used. The ending slash will be trimmed.</param>
    public void SetConfigFolder(string configDir)
    {
      if (configDir == null)
      {
        return;
      }

      try
      {
        configDir = configDir.TrimEnd('\\');
        if (!Directory.Exists(configDir))
        {
          Directory.CreateDirectory(configDir);
        }
      }
      catch (Exception e)
      {
        log.Error($"Couldn't set the configuration directory to: {configDir}, Error message: {e.Message}");
      }
      finally
      {
        this.ConfigFolder = configDir;
      }
    }

    /// <summary>
    /// Loads the or creates the configuration.
    /// </summary>
    /// <returns>The existing or new configuration.</returns>
    public ApplicationConfiguration LoadOrCreateConfiguration()
    {
      if (File.Exists(this.ConfigFile))
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
    /// <param name="configFilePath">The configuration file path.</param>
    /// <exception cref="InvalidOperationException">Couldn't get the directory for configuration file.</exception>
    public void SaveConfiguration(ApplicationConfiguration configuration, string configFilePath = null)
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

    public async Task<TModel> GetJsonModel<TModel>(string requestUrl, CancellationToken token = default(CancellationToken), Action<WebRequest> requestCallback = null)
     where TModel : class
    {
      const string JSON_CONTENT_TYPE = "application/json";
      WebRequest.DefaultWebProxy = null;
      var request = (HttpWebRequest)WebRequest.Create(requestUrl);
      request.Accept = JSON_CONTENT_TYPE;
      request.ContentType = JSON_CONTENT_TYPE;

      requestCallback?.Invoke(request);

      try
      {
        log.Trace($"Fetching JSON object ({typeof(TModel)}) from \"{requestUrl}\" ...");
        using (token.Register(() => request.Abort(), false))
        using (var response = (HttpWebResponse)request.GetResponse())
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

        log.Error($"Could not fetch JSON data from {requestUrl}.", ex);
        return null;
      }
    }

    /// <summary>
    /// Handles the configuration errors.
    /// </summary>
    /// <param name="appConfiguration">The application configuration.</param>
    private void HandleConfigurationErrors(ApplicationConfiguration appConfiguration)
    {
      this.CheckConnectorsConfigurationDoNotContainNull(appConfiguration);
      this.HandleOlderConfigurationFiles(appConfiguration);
    }

    /// <summary>
    /// Handles incorrectly loading CC connectors.
    /// </summary>
    /// <param name="applicationConfiguration">Application configuration to check for errors.</param>
    /// <returns>True if made changes, false if no changes made. Changes made implies that configuration was incorrect.</returns>
    private bool HandleCruiseControlNewProperties(ApplicationConfiguration applicationConfiguration)
    {
      bool changesMade = false;
      foreach (var connector in applicationConfiguration.ConnectorsConfiguration)
      {
        if (connector.Type == "CruiseControl")
        {
          bool hasDirectAddress = false;
          string address = string.Empty;
          foreach (var item in connector.ConfigurationItems)
          {
            if (item.Key == "DirectAddress")
            {
              hasDirectAddress = true;
            }

            if (item.Key == "Address")
            {
              address = item.Value;
            }
          }

          if (!hasDirectAddress)
          {
            connector.ConfigurationItems.Add(new ConfigurationItem("DirectAddress", address));
            changesMade = true;
            foreach (var item in connector.ConfigurationItems)
            {
              if (item.Key == "Address")
              {
                item.Value = address.Split(new string[] { "server/" }, StringSplitOptions.None)[0];
              }
            }
          }
        }
      }

      return changesMade;
    }

    /// <summary>
    /// Checks if configuration is old/deprecated and calls functions that make changes to those config versions.
    /// </summary>
    /// <param name="applicationConfiguration">Configuration to be checked.</param>
    private void HandleOlderConfigurationFiles(ApplicationConfiguration applicationConfiguration)
    {
      this.HandleCruiseControlNewProperties(applicationConfiguration);
      return;
    }

    /// <summary>
    /// Checks the connectors configuration do not contain null and removes the configuration if it was null.
    /// </summary>
    /// <param name="appConfiguration">The application configuration.</param>
    private void CheckConnectorsConfigurationDoNotContainNull(ApplicationConfiguration appConfiguration)
    {
      for (var i = 0; i < appConfiguration.ConnectorsConfiguration.Count; i++)
      {
        if (appConfiguration.ConnectorsConfiguration[i] == null)
        {
          log.Error("One of the project configuration entries was null");
          appConfiguration.ConnectorsConfiguration.RemoveAt(i);
          i--;
        }
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