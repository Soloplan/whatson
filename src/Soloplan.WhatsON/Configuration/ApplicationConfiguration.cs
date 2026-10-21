// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationConfiguration.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Configuration
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Newtonsoft.Json;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Holds the serializable configuration.
  /// </summary>
  public class ApplicationConfiguration
  {
    private ApplicationTheme? legacyThemeMode;

    private ApplicationTheme? themeMode;

    /// <summary>
    /// Gets or sets a value indicating whether App is always on top.
    /// </summary>
    public bool AlwaysOnTop { get; set; }

    /// <summary>
    /// Gets the connectors configuration.
    /// </summary>
    public IList<ConnectorConfiguration> ConnectorsConfiguration { get; } = new List<ConnectorConfiguration>();

    /// <summary>
    /// Gets credentials shared by projects on the same server.
    /// </summary>
    public IList<ConnectorConfiguration> ServerConfigurations { get; } = new List<ConnectorConfiguration>();

    /// <summary>
    /// Gets the saved groups in display order, including named groups without projects.
    /// </summary>
    public IList<string> Groups { get; } = new List<string>();

    /// <summary>
    /// Gets or sets the interface language. The default follows supported Windows languages.
    /// </summary>
    public ApplicationLanguage Language { get; set; } = ApplicationLanguage.System;

    /// <summary>
    /// Gets the notification configuration.
    /// </summary>
    public NotificationConfiguration NotificationConfiguration { get; } = new NotificationConfiguration();

    /// <summary>
    /// Gets or sets a value indicating whether App is starts up minimized.
    /// </summary>
    public bool OpenMinimized { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether icon on taskbar should be shown.
    /// </summary>
    public bool ShowInTaskbar { get; set; }

    /// <summary>
    /// Gets or sets the appearance preference. New configurations follow Windows.
    /// </summary>
    public ApplicationTheme ThemeMode
    {
      get => this.themeMode ?? this.legacyThemeMode ?? ApplicationTheme.System;
      set => this.themeMode = value;
    }

    /// <summary>
    /// Gets or sets a view style, for now normal/compact are available and they only control the spacing of items.
    /// </summary>
    public ViewStyle ViewStyle { get; set; }

    // Read old files without writing the retired setting or overriding an explicit theme.
    [JsonProperty("DarkThemeEnabled")]
    private bool LegacyDarkThemeEnabled
    {
      set => this.legacyThemeMode = value ? ApplicationTheme.Dark : ApplicationTheme.Light;
    }

    /// <summary>
    /// Migrates legacy project credentials and attaches each project to its server settings.
    /// </summary>
    public void InitializeServerConfigurations()
    {
      foreach (var connector in this.ConnectorsConfiguration)
      {
        var keys = PluginManager.Instance.GetPlugin(connector)?.ConnectorType
          .GetCustomAttributes(typeof(ConfigurationItemAttribute), true).Cast<ConfigurationItemAttribute>()
          .Where(attribute => attribute.IsServerSetting).Select(attribute => attribute.Key).ToList();
        var normalizedAddress = GetServerAddress(connector);
        if (keys == null || keys.Count == 0 || normalizedAddress == null)
        {
          connector.ServerConfiguration = keys?.Count > 0 ? new ConnectorConfiguration(connector.Type) : null;
          continue;
        }

        var server = this.ServerConfigurations.FirstOrDefault(item => item.Type == connector.Type
          && GetServerAddress(item) == normalizedAddress);
        if (server == null)
        {
          var legacySource = this.ConnectorsConfiguration.Where(item => item.Type == connector.Type && item.ServerConfiguration == null
            && GetServerAddress(item) == normalizedAddress)
            .FirstOrDefault(item => keys.All(key => !string.IsNullOrWhiteSpace(item.ConfigurationItems.FirstOrDefault(setting => setting.Key == key)?.Value)));
          server = new ConnectorConfiguration(connector.Type, normalizedAddress, Connector.ServerAddress, normalizedAddress);
          foreach (var key in keys)
          {
            server.GetConfigurationByKey(key).Value = legacySource?.ConfigurationItems.FirstOrDefault(item => item.Key == key)?.Value;
          }

          this.ServerConfigurations.Add(server);
        }

        foreach (var key in keys)
        {
          var shared = server.GetConfigurationByKey(key);

          // Keep conflicting legacy values so migration never destroys another saved account.
          foreach (var legacy in connector.ConfigurationItems.Where(item => item.Key == key
            && (string.IsNullOrEmpty(item.Value) || item.Value == shared.Value)).ToList())
          {
            connector.ConfigurationItems.Remove(legacy);
          }
        }

        connector.ServerConfiguration = server;
      }
    }

    /// <summary>
    /// Gets saved groups together with groups inferred from projects in older configurations.
    /// </summary>
    /// <returns>The distinct group names in display order.</returns>
    public IEnumerable<string> GetGroupNames()
    {
      var projectGroups = this.ConnectorsConfiguration
        .Select(configuration => configuration.GetConfigurationByKey(Connector.Category).Value?.Trim() ?? string.Empty).ToList();
      return this.Groups
        .Where(name => !string.IsNullOrWhiteSpace(name) || (name == string.Empty && projectGroups.Contains(string.Empty)))
        .Select(name => name.Trim())
        .Concat(projectGroups)
        .Distinct();
    }

    /// <summary>
    /// Gets the notification configuration from the per connector settings or the global settings..
    /// </summary>
    /// <param name="connectorConfiguration">The connector configuration.</param>
    /// <returns>The notification configuration.</returns>
    public NotificationConfiguration GetNotificationConfiguration(ConnectorConfiguration connectorConfiguration)
    {
      // firstly check if the config item exists to prevent creation it "on access".
      var notificationVisibilityConfigItem =
        connectorConfiguration.ConfigurationItems.FirstOrDefault(ci => ci.Key == Connector.NotificationsVisbility);
      if (notificationVisibilityConfigItem == null)
      {
        return this.NotificationConfiguration;
      }

      var connectorNotificationConfiguration = notificationVisibilityConfigItem.GetOrCreateConnectorNotificationConfiguration();
      return connectorNotificationConfiguration.UseGlobalNotificationSettings ? this.NotificationConfiguration : connectorNotificationConfiguration;
    }

    private static string? GetServerAddress(ConnectorConfiguration connector)
    {
      var address = connector.ConfigurationItems.FirstOrDefault(item => item.Key == Connector.ServerAddress)?.Value;
      return Uri.TryCreate(address?.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) ? uri.AbsoluteUri.TrimEnd('/') : null;
    }
  }
}
