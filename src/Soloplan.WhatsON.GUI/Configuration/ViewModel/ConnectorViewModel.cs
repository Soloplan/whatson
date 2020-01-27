// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectorViewModel.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.ViewModel
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// The view model for see <see cref="Connector"/>.
  /// </summary>
  public class ConnectorViewModel : ViewModelBase, IConfigurationItemProvider
  {
    /// <summary>
    /// The name of the connector.
    /// </summary>
    private string name;

    /// <summary>
    /// The source connector.
    /// </summary>
    private ConnectorConfiguration sourceConnectorConfiguration;

    /// <summary>
    /// The source connector Plugin.
    /// </summary>
    private ConnectorPlugin sourceConnectorPlugin;

    /// <summary>
    /// Gets the connector source configuration.
    /// </summary>
    public ConnectorConfiguration SourceConnectorConfiguration
    {
      get => this.sourceConnectorConfiguration;
      private set
      {
        this.sourceConnectorConfiguration = value;
        this.OnPropertyChanged();
        this.OnPropertyChanged(nameof(this.SourceConnectorPlugin));
      }
    }

    /// <summary>
    /// Gets or sets the source connector plugin.
    /// </summary>
    public ConnectorPlugin SourceConnectorPlugin
    {
      get
      {
        if (this.sourceConnectorPlugin == null && this.SourceConnectorConfiguration != null)
        {
          this.sourceConnectorPlugin = PluginManager.Instance.GetPlugin(this.SourceConnectorConfiguration);
        }

        return this.sourceConnectorPlugin;
      }

      set => this.sourceConnectorPlugin = value;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public Guid Identifier { get; private set; }

    /// <summary>
    /// Gets or sets the name of the connector.
    /// </summary>
    public string Name
    {
      get => this.name;
      set
      {
        this.name = value;
        this.OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets the full name of this connector including the category (if set).
    /// </summary>
    public string FullName
    {
      get
      {
        var group = this.ConfigurationItems.FirstOrDefault(x => x?.Key != null && x.Key == Connector.Category)?.Value;
        if (!string.IsNullOrWhiteSpace(group))
        {
          return $"{group} > {this.Name}";
        }

        return this.Name;
      }
    }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    public List<ConfigurationItemViewModel> ConfigurationItems { get; private set; }

    /// <summary>
    /// Loads the connector view model from the source object.
    /// </summary>
    /// <param name="connectorSource">The connector source.</param>
    public void Load(ConnectorConfiguration connectorSource)
    {
      this.IsLoaded = false;
      try
      {
        this.SourceConnectorConfiguration = connectorSource;
        if (this.ConfigurationItems == null)
        {
          this.ConfigurationItems = new List<ConfigurationItemViewModel>();
        }

        if (connectorSource != null)
        {
          this.Name = connectorSource.Name;
          this.Identifier = connectorSource.Identifier;
          var configurationItemsToNull = this.ConfigurationItems.Where(cvm => this.SourceConnectorConfiguration.ConfigurationItems.All(c => c.Key != cvm.Key));
          foreach (var configurationItemToNull in configurationItemsToNull)
          {
            configurationItemToNull.Value = null;
          }

          foreach (var configItem in connectorSource.ConfigurationItems)
          {
            var configItemViewModel = this.GetConfigurationItemViewModel(configItem.Key);
            configItemViewModel.Load(configItem);
          }
        }
      }
      finally
      {
        this.IsLoaded = true;
      }
    }

    /// <summary>
    /// Gets the connector configuration attributes.
    /// </summary>
    /// <returns>The connector attributes.</returns>
    public IList<ConfigurationItemAttribute> GetConnectorConfigAttributes()
    {
      var configurationItemAttributes = this.SourceConnectorPlugin.ConnectorType.GetCustomAttributes(typeof(ConfigurationItemAttribute), true).Cast<ConfigurationItemAttribute>().ToList();
      foreach (var configurationItemAttribute in configurationItemAttributes)
      {
        ConfigResourcesHelper.ApplyConfigResources(configurationItemAttribute, this.SourceConnectorPlugin.ConnectorType);
      }

      return configurationItemAttributes;
    }

    /// <summary>
    /// Gets (and might also create if it does not exists) the configuration view model by key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The configuration view model.</returns>
    public IConfigurationItem GetConfigurationByKey(string key)
    {
      var configItem = this.GetConfigurationItemViewModel(key);
      configItem.Load(key);
      return configItem;
    }

    /// <summary>
    /// Applies changes to source connector configuration.
    /// </summary>
    /// <param name="newConnectorConfigurationCreated">if set to <c>true</c> [new connector configuration created].</param>
    /// <returns>New instance of <see cref="ConnectorConfiguration"/> if it was created; otherwise the existing instance.</returns>
    public ConnectorConfiguration ApplyToSourceConnectorConfiguration(out bool newConnectorConfigurationCreated)
    {
      newConnectorConfigurationCreated = false;

      if (this.SourceConnectorConfiguration == null)
      {
        newConnectorConfigurationCreated = true;
        this.SourceConnectorConfiguration = this.CreateNewConnectorConfiguration();
      }
      else
      {
        this.SourceConnectorConfiguration.Name = this.name;
        foreach (var configurationItem in this.ConfigurationItems)
        {
          var appliedConfigurationItem = configurationItem.ApplyToSource(out bool newItemCreated);
          if (newItemCreated)
          {
            this.SourceConnectorConfiguration.ConfigurationItems.Add(appliedConfigurationItem);
          }
        }
      }

      return this.SourceConnectorConfiguration;
    }

    /// <summary>
    /// Creates the new connector configuration with applied changes.
    /// </summary>
    /// <returns>New instance of <see cref="ConnectorConfiguration"/>.</returns>
    public ConnectorConfiguration CreateNewConnectorConfiguration()
    {
      var sourceConnectorConfig = new ConnectorConfiguration(this.SourceConnectorPlugin.Name);
      sourceConnectorConfig.Name = this.name;
      foreach (var configurationItem in this.ConfigurationItems)
      {
        var newConfigurationItem = configurationItem.CreateNewConfigurationItem();
        sourceConnectorConfig.ConfigurationItems.Add(newConfigurationItem);
      }

      return sourceConnectorConfig;
    }

    /// <summary>
    /// Gets the configuration item view model by the given key.
    /// In case it is not existing, a new item is created.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The found or created <see cref="ConfigurationItemViewModel"/>.</returns>
    private ConfigurationItemViewModel GetConfigurationItemViewModel(string key)
    {
      var configItemViewModel = this.ConfigurationItems.FirstOrDefault(x => x.Key == key);
      if (configItemViewModel == null)
      {
        configItemViewModel = new ConfigurationItemViewModel();
        configItemViewModel.PropertyChanged += (s, e) =>
        {
          if (configItemViewModel.IsLoaded)
          {
            this.OnPropertyChanged(nameof(this.ConfigurationItems));
          }
        };

        this.ConfigurationItems.Add(configItemViewModel);
      }

      return configItemViewModel;
    }
  }
}