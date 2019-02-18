// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationItemViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.ViewModel
{
  using System;

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
    /// The value.s
    /// </summary>
    private string value;

    /// <summary>
    /// Gets the key.
    /// </summary>
    public string Key
    {
      get => this.CheckIsLoadedAndGetValue(() => this.key);
      private set => this.key = value;
    }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string Value
    {
      get => this.CheckIsLoadedAndGetValue(() => this.value);
      set => this.value = value;
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
      this.ConfigurationItem = configurationItemSource;
      this.Key = configurationItemSource.Key;
      this.Value = configurationItemSource.Value;

      this.IsLoaded = true;
    }

    /// <summary>
    /// Loads the configuration item view model from the source object.
    /// </summary>
    /// <param name="itemKey">The key.</param>
    public void Load(string itemKey)
    {
      this.Key = itemKey;
      this.IsLoaded = true;
    }
  }
}