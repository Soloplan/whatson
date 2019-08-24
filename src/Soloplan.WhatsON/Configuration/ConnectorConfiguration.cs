// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectorConfiguration.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Configuration
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Newtonsoft.Json;

  /// <summary>
  /// Represents the configuration of a connector.
  /// </summary>
  public class ConnectorConfiguration
  {
    private string type;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorConfiguration"/> class.
    /// </summary>
    /// <param name="connectorName">Name of the type.</param>
    /// <param name="name">The name.</param>
    /// <param name="configurationItems">The configuration items.</param>
    public ConnectorConfiguration(string connectorName, string name, List<ConfigurationItem> configurationItems)
      : this(connectorName, name)
    {
      this.ConfigurationItems = configurationItems;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorConfiguration"/> class.
    /// </summary>
    /// <param name="connectorName">Name of the type.</param>
    /// <param name="name">The name.</param>
    /// <param name="key1">The key1.</param>
    /// <param name="value1">The value1.</param>
    /// <param name="key2">The key2.</param>
    /// <param name="value2">The value2.</param>
    /// <param name="key3">The key3.</param>
    /// <param name="value3">The value3.</param>
    public ConnectorConfiguration(string connectorName, string name, string key1, string value1, string key2 = null, string value2 = null, string key3 = null, string value3 = null)
    : this(connectorName, name)
    {
      this.Name = name;
      this.ConfigurationItems = new List<ConfigurationItem>();
      this.ConfigurationItems.Add(new ConfigurationItem(key1, value1));
      if (key2 != null)
      {
        this.ConfigurationItems.Add(new ConfigurationItem(key2, value2));
      }

      if (key2 != null && key3 != null)
      {
        this.ConfigurationItems.Add(new ConfigurationItem(key3, value3));
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorConfiguration"/> class.
    /// </summary>
    /// <param name="connectorName">Name of the type.</param>
    /// <param name="name">The name.</param>
    public ConnectorConfiguration(string connectorName, string name)
     : this(connectorName)
    {
      this.Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorConfiguration"/> class.
    /// </summary>
    /// <param name="connectorName">Name of the plugin type.</param>
    [JsonConstructor]
    public ConnectorConfiguration(string connectorName)
    {
      this.Type = connectorName;
      this.Identifier = Guid.NewGuid();
    }

    /// <summary>
    /// Gets or sets the configuration identifier.
    /// </summary>
    public Guid Identifier { get; set; }

    /// <summary>
    /// Gets or sets the name of the connector.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the name of the plugin type.
    /// </summary>
    public string Type
    {
      get
      {
        // hardcoded support for configuration prior to v0.9.0-beta
        // TODO: Drop this support with the 1.0.0 release
        if (string.IsNullOrWhiteSpace(this.type) && !string.IsNullOrWhiteSpace(this.PluginTypeName))
        {
          if (this.PluginTypeName.Contains("Jenkins"))
          {
            return "Jenkins";
          }

          if (this.PluginTypeName.Contains("CruiseControl"))
          {
            return "CruiseControl";
          }
        }

        return this.type;
      }

      set
      {
        this.type = value;
      }
    }

    /// <summary>
    /// Gets or sets the name of the plugin type.
    /// </summary>
    [Obsolete("Replaced by the Type which is evaluated from the ConnectorPlugin.Name property instead of the FullName of the Plugin type.")]
    public string PluginTypeName { internal get; set; }

    /// <summary>
    /// Gets or sets the configuration items.
    /// </summary>
    public IList<ConfigurationItem> ConfigurationItems { get; set; } = new List<ConfigurationItem>();

    /// <summary>
    /// Gets the configuration by key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The configuration item.</returns>
    public ConfigurationItem GetConfigurationByKey(string key)
    {
      var configItem = this.ConfigurationItems.FirstOrDefault(x => x.Key == key);
      if (configItem == null)
      {
        configItem = new ConfigurationItem(key);
        this.ConfigurationItems.Add(configItem);
        return configItem;
      }

      return this.ConfigurationItems.FirstOrDefault(x => x.Key == key);
    }
  }
}