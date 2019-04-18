// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationItemViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.ViewModel
{
  /// <summary>
  /// The view model for see <see cref="ConfigurationItem"/>.
  /// </summary>
  public class ConfigurationItemViewModel : ViewModelBase
  {
    /// <summary>
    /// The key.
    /// </summary>
    private string key;

    /// <summary>
    /// The value.
    /// </summary>
    private string value;

    /// <summary>
    /// Gets the key.
    /// </summary>
    public string Key
    {
      get => this.key;
      private set
      {
        if (this.key == value)
        {
          return;
        }

        this.key = value;
        this.OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string Value
    {
      get => this.value;
      set
      {
        if (this.value == value)
        {
          return;
        }

        this.value = value;
        this.OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets the original configuration item.
    /// </summary>
    public ConfigurationItem ConfigurationItem { get; private set; }

    /// <summary>
    /// Loads the configuration item view model from the source object.
    /// </summary>
    /// <param name="configurationItemSource">The configuration item source.</param>
    public void Load(ConfigurationItem configurationItemSource)
    {
      this.IsLoaded = false;
      try
      {
        this.ConfigurationItem = configurationItemSource;
        this.Key = configurationItemSource.Key;
        this.Value = configurationItemSource.Value;
      }
      finally
      {
        this.IsLoaded = true;
      }
    }

    /// <summary>
    /// Loads the configuration item view model from the source object.
    /// </summary>
    /// <param name="itemKey">The key.</param>
    public void Load(string itemKey)
    {
      this.IsLoaded = false;
      try
      {
        this.Key = itemKey;
      }
      finally
      {
        this.IsLoaded = true;
      }
    }

    public static ConfigurationItem CreateNewConfigurationItem(ConfigurationItemViewModel configurationItemViewModel, out bool newItemCreated)
    {
      newItemCreated = true;
      var result = new ConfigurationItem(configurationItemViewModel.key);
      result.Value = configurationItemViewModel.value;
      return result;
    }

    /// <summary>
    /// Applies modifications to source.
    /// </summary>
    /// <param name="newItemCreated">if set to <c>true</c> a new item was created.</param>
    public static ConfigurationItem ApplyToSource(ConfigurationItemViewModel configurationItemViewModel,  out bool newItemCreated)
    {
      newItemCreated = false;
      if (configurationItemViewModel.ConfigurationItem == null)
      {
        configurationItemViewModel.ConfigurationItem = new ConfigurationItem(configurationItemViewModel.key);
        newItemCreated = true;
      }

      configurationItemViewModel.ConfigurationItem.Value = configurationItemViewModel.value;
      return configurationItemViewModel.ConfigurationItem;
    }
  }
}