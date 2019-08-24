// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationItem.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Configuration
{
  using Newtonsoft.Json;

  /// <summary>
  /// The configuration item.
  /// </summary>
  public class ConfigurationItem : IConfigurationItem
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationItem"/> class.
    /// </summary>
    /// <param name="key">The key.</param>
    [JsonConstructor]
    public ConfigurationItem(string key)
    {
      this.Key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationItem" /> class.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public ConfigurationItem(string key, string value)
    {
      this.Key = key;
      this.Value = value;
    }

    /// <summary>
    /// Gets the key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string Value { get; set; }
  }
}