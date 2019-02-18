// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationItem.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  /// <summary>
  /// The configuration item.
  /// </summary>
  public class ConfigurationItem
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationItem"/> class.
    /// </summary>
    /// <param name="key">The key.</param>
    public ConfigurationItem(string key)
    {
      this.Key = key;
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