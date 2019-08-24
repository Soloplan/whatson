// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConfigurationItem.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Configuration
{
  /// <summary>
  /// The configuration item.
  /// </summary>
  public interface IConfigurationItem
  {
    /// <summary>
    /// Gets the key.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    string Value { get; set; }
  }
}